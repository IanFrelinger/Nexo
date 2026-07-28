#!/usr/bin/env python3
"""Run P12/P16/P17 onboarding sequentially and emit compact evidence JSON."""
import argparse, json, time, urllib.request
from pathlib import Path

CASES = {
    "P12": ("P12_InternalDeps", "qt-peripheral"),
    "P16": ("P16_NestedRelIncludes", "no-qt"),
    "P17": ("P17_QThreadPeripheral", "qt-peripheral"),
}

def main() -> int:
    p=argparse.ArgumentParser()
    p.add_argument("--url", default="http://127.0.0.1:5237")
    p.add_argument("--projects", type=Path, required=True)
    p.add_argument("--sample", type=Path, required=True)
    p.add_argument("--output", type=Path, required=True)
    a=p.parse_args(); a.output.mkdir(parents=True, exist_ok=True)
    failed=False
    for case,(folder,truth) in CASES.items():
        body={"srcDir":str(a.projects/folder),"samples":[str(a.sample)],
              "outlineFiles":0,"minEvents":25,"preferScaffold":True}
        started=time.monotonic()
        req=urllib.request.Request(a.url+"/api/onboard",
            data=json.dumps(body).encode(),headers={"Content-Type":"application/json"})
        try:
            with urllib.request.urlopen(req,timeout=900) as response:
                result=json.load(response)
        except Exception as exc:
            result={"ok":False,"error":str(exc)}
        compact={"project":case,"ok":bool(result.get("ok")),
                 "triage":result.get("triage"),"ground_truth":truth,
                 "oracle_rows":result.get("oracleRows"),
                 "duration_s":round(time.monotonic()-started,3),
                 "error":result.get("error")}
        (a.output/f"{case}.json").write_text(json.dumps(compact,indent=2)+"\n")
        print(json.dumps(compact))
        if not compact["ok"] or compact["oracle_rows"] != 25 or compact["triage"] != truth:
            failed=True
    return int(failed)
if __name__=="__main__": raise SystemExit(main())
