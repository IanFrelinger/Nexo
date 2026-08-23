#!/usr/bin/env python3
"""SmolVLM2 Video Understanding API for Ashlar. Model runs in Docker; C# service calls this."""
import os
import tempfile
import time
from pathlib import Path

from fastapi import FastAPI, File, Form, HTTPException, UploadFile
from pydantic import BaseModel

_model = None
_processor = None
_device = "cpu"


def get_model():
    global _model, _processor, _device
    if _model is None:
        import torch
        from transformers import AutoModelForImageTextToText, AutoProcessor

        _device = "cuda" if torch.cuda.is_available() else "cpu"
        model_name = os.environ.get("SMOLVLM_MODEL", "HuggingFaceTB/SmolVLM2-500M-Video-Instruct")
        print(f"Loading {model_name} on {_device}...")
        _processor = AutoProcessor.from_pretrained(model_name, trust_remote_code=True)
        _model = AutoModelForImageTextToText.from_pretrained(
            model_name,
            torch_dtype=torch.bfloat16 if _device == "cuda" else torch.float32,
            trust_remote_code=True,
        ).to(_device)
        if _device == "cpu":
            _model = _model.float()
        print("Model loaded.")
    return _model, _processor, _device


app = FastAPI(title="SmolVLM2 Video Service", version="1.0.0")


class AnalyzeResponse(BaseModel):
    summary: str
    model: str
    device: str
    elapsed_ms: float


@app.get("/health")
async def health():
    return {"status": "ok", "service": "smolvlm2-video"}


@app.post("/v1/analyze", response_model=AnalyzeResponse)
async def analyze_video(
    video: UploadFile = File(...),
    prompt: str = Form("Summarize what is happening in this video."),
    system_prompt: str = Form(""),
):
    if not video.filename or not video.filename.lower().endswith((".mp4", ".mov", ".avi", ".webm")):
        raise HTTPException(400, "Expected video file (mp4, mov, avi, webm)")
    try:
        with tempfile.NamedTemporaryFile(suffix=".mp4", delete=False) as f:
            f.write(await video.read())
            tmp_path = f.name
    except Exception as e:
        raise HTTPException(500, f"Failed to save upload: {e}")

    try:
        import torch

        model, processor, device = get_model()
        model_name = os.environ.get("SMOLVLM_MODEL", "HuggingFaceTB/SmolVLM2-500M-Video-Instruct")

        messages = []
        if system_prompt.strip():
            messages.append({"role": "system", "content": system_prompt})
        messages.append(
            {
                "role": "user",
                "content": [
                    {"type": "video", "path": tmp_path},
                    {"type": "text", "text": prompt},
                ],
            }
        )

        inputs = processor.apply_chat_template(
            messages,
            add_generation_prompt=True,
            tokenize=True,
            return_dict=True,
            return_tensors="pt",
        )
        inputs = {k: v.to(device) if hasattr(v, "to") else v for k, v in inputs.items()}
        if device == "cuda":
            inputs = {k: (v.to(torch.bfloat16) if v.dtype == torch.float32 and hasattr(v, "to") else v) for k, v in inputs.items()}

        start = time.perf_counter()
        with torch.no_grad():
            generated_ids = model.generate(**inputs, do_sample=False, max_new_tokens=256, pad_token_id=processor.tokenizer.eos_token_id)
        elapsed = (time.perf_counter() - start) * 1000

        generated_text = processor.batch_decode(generated_ids, skip_special_tokens=True)
        summary = generated_text[0].strip() if generated_text else ""

        return AnalyzeResponse(summary=summary, model=model_name, device=device, elapsed_ms=elapsed)
    except Exception as e:
        raise HTTPException(500, f"Inference failed: {str(e)}")
    finally:
        try:
            Path(tmp_path).unlink(missing_ok=True)
        except Exception:
            pass


if __name__ == "__main__":
    import uvicorn
    port = int(os.environ.get("PORT", "8080"))
    uvicorn.run(app, host="0.0.0.0", port=port)
