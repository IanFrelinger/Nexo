# 🎮 Interactive Nexo Agent Foundry Demo Guide

## 🚀 **Complete Setup & Usage Guide**

This guide walks you through the complete interactive demo experience where you act as a **Project Manager/Designer** and task the Nexo Agent with project generation, testing, and validation.

---

## 📋 **Prerequisites**

- **Unity 2022.3.62f1** or later installed
- **.NET 8.0 SDK** installed
- **Terminal/Command Line** access
- **Interactive Terminal** (for full interactive demo)

---

## 🎯 **Step 1: Bootstrap Unity Project**

### **Automated Bootstrap**
```bash
# Navigate to Nexo repo
cd /path/to/Nexo

# Run bootstrap script (replace with your Unity path)
./scripts/unity-bootstrap.sh ~/UnityProjects/NexoAgentDemo "/Applications/UnityEngineVersions/2022.3.62f1/Unity.app/Contents/MacOS/Unity"
```

### **Manual Setup** (if bootstrap fails)
```bash
# Create Unity project
mkdir -p ~/UnityProjects/NexoAgentDemo
cd ~/UnityProjects/NexoAgentDemo

# Add Nexo packages to manifest.json
python3 -c "
import json
with open('Packages/manifest.json', 'r') as f:
    data = json.load(f)
deps = data.setdefault('dependencies', {})
nexo_repo = '/path/to/Nexo/Packages'
deps['com.nexo.agent.unity'] = f'file:{nexo_repo}/com.nexo.agent.unity'
deps['com.nexo.agent.tools'] = f'file:{nexo_repo}/com.nexo.agent.tools'
deps['com.nexo.agent.validation'] = f'file:{nexo_repo}/com.nexo.agent.validation'
with open('Packages/manifest.json', 'w') as f:
    json.dump(data, f, indent=2)
"
```

---

## 🎮 **Step 2: Open Unity Project**

### **Open Unity**
```bash
# Open Unity with the project
open -a "/Applications/UnityEngineVersions/2022.3.62f1/Unity.app" ~/UnityProjects/NexoAgentDemo
```

### **Wait for Import**
- Unity will automatically import the Nexo packages
- Wait for the import to complete (may take 2-3 minutes)
- Look for "Nexo" menu items in the Unity Editor

---

## 🤖 **Step 3: Run Interactive Project Manager Demo**

### **Interactive Mode** (Real Terminal Required)
```bash
# Navigate to Nexo repo
cd /path/to/Nexo

# Run interactive demo
dotnet run --project src/Nexo.Agent.Demo.ProjectManager
```

### **Non-Interactive Demo Mode** (Automated Showcase)
```bash
# Run automated demo
dotnet run --project src/Nexo.Agent.Demo.ProjectManager -- --demo
```

---

## 🎯 **Step 4: Interactive Demo Workflow**

### **Main Menu Options**
When running the interactive demo, you'll see:

```
╭────────────────────Nexo Project Manager────────────────────╮
│ What would you like to do?                                 │
│                                                          │
│ 📋 Create New Project Task                               │
│ 🎯 Assign Task to Agent                                  │
│ 📊 Review Project Status                                 │
│ 🔍 Run Validation Tests                                  │
│ 📈 Generate Project Report                               │
│ 🛠️ Configure Agent Settings                             │
│ 📁 View Project History                                  │
│ ❌ Exit                                                  │
╰──────────────────────────────────────────────────────────╯
```

### **1. Create New Project Task**
- **Task Name**: "Implement Player Controller"
- **Description**: "Create FPS player with movement, look, and jump"
- **Priority**: Critical/High/Medium/Low
- **Type**: Development/Testing/Analysis/Validation/Documentation
- **Estimated Effort**: Hours

### **2. Assign Task to Agent**
- Select from available unassigned tasks
- Agent automatically takes over and executes
- Real-time progress updates
- Automatic completion with effort tracking

### **3. Review Project Status**
- Live dashboard of all tasks
- Completion rates and effort tracking
- Priority-based task management
- Real-time status updates

### **4. Run Validation Tests**
- **Visual Validation**: UI/UX analysis
- **Gameplay Testing**: Core mechanics validation
- **Accessibility Testing**: WCAG compliance
- **Performance Testing**: Frame rate and optimization
- **Security Testing**: Vulnerability assessment

### **5. Generate Project Report**
- Comprehensive project metrics
- Validation scores and trends
- Actionable recommendations
- Effort analysis and forecasting

---

## 🎮 **Step 5: Unity Integration**

### **Agent Workbench** (Unity Editor)
1. **Open Agent Workbench**: `Nexo > Agent Workbench`
2. **Create Feature Spec**: Define game requirements
3. **Generate Assets**: Let Agent create scripts, prefabs, scenes
4. **Validate Results**: Run built-in validation agents

### **Visual Validation Window** (Unity Editor)
1. **Open Visual Validation**: `Nexo > Visual Validation`
2. **Capture Screenshots**: Take game view screenshots
3. **Analyze with OLLama**: AI-powered visual analysis
4. **Get Recommendations**: Actionable improvement suggestions

### **Built-in Tools Available**
- **PlayerController**: FPS movement and controls
- **Shotgun**: Weapon system with hitscan
- **EnemyImp**: NavMesh AI with patrol/chase
- **DoorKeySystem**: Interactive doors and keys
- **GameHUD**: UIToolkit-based UI
- **BlockoutBuilder**: Procedural level generation

---

## 🔧 **Step 6: Agent Configuration**

### **Agent Modes**
- **OFF**: Rule-based planning, no cloud dependencies
- **HYBRID**: Optional cloud integration with offline fallback
- **EMBEDDED**: Full cloud integration for advanced features

### **Validation Agents**
- **Playbot**: Input System test automation
- **UIValidator**: UIToolkit accessibility validation
- **PerfGuard**: Performance monitoring and optimization
- **NavGuard**: Navigation mesh validation
- **CodeGate**: Policy enforcement and quality gates

---

## 📊 **Step 7: Project Templates**

### **Available Templates**
- **🎮 Unity Game Demo**: Complete FPS game with AI
- **🌐 Web Application**: Full-stack web development
- **📱 Mobile App**: Cross-platform mobile development
- **🤖 AI Service**: ML model integration
- **📊 Data Pipeline**: Analytics and data processing
- **🔧 DevOps Tool**: Automation and deployment

---

## 🎯 **Example Interactive Session**

### **Scenario: Create Unity FPS Game**

1. **Create Tasks**:
   - "Implement Player Controller" (Critical, 8h)
   - "Create Enemy AI" (High, 12h)
   - "Build UI System" (Medium, 6h)
   - "Performance Optimization" (High, 4h)

2. **Assign to Agent**:
   - Agent takes over each task
   - Real-time progress updates
   - Automatic completion with validation

3. **Review Status**:
   - 4 tasks completed
   - 30 hours total effort
   - 100% completion rate

4. **Run Validation**:
   - Visual: 85% (Good)
   - Gameplay: 92% (Excellent)
   - Performance: 88% (Good)
   - Accessibility: 75% (Fair)

5. **Generate Report**:
   - Overall score: 85%
   - 2 high-priority issues
   - 5 actionable recommendations

---

## 🚀 **Advanced Features**

### **OLLama Visual Analytics**
- **Setup**: Install OLLama with `llava:7b` model
- **Usage**: Agent automatically uses for visual validation
- **Benefits**: AI-powered game analysis and recommendations

### **Self-Healing Pipeline**
- **Policy Violations**: Automatic detection and repair
- **Quality Gates**: SAST/SCA/license enforcement
- **Hot Reloading**: Tools updated without restart

### **Real-time Monitoring**
- **OpenTelemetry**: Distributed tracing and metrics
- **Performance**: Frame time and allocation tracking
- **Validation**: Continuous quality assurance

---

## 🎉 **Success Indicators**

### **Project Manager Demo**
- ✅ All tasks completed successfully
- ✅ Agent executed tasks autonomously
- ✅ Validation tests passed
- ✅ Comprehensive reports generated

### **Unity Integration**
- ✅ Agent Workbench accessible
- ✅ Visual Validation working
- ✅ Built-in tools functional
- ✅ Validation agents running

### **Full Workflow**
- ✅ Project creation to completion
- ✅ Real-time monitoring and reporting
- ✅ Quality assurance and validation
- ✅ Actionable recommendations provided

---

## 🆘 **Troubleshooting**

### **Unity Import Issues**
```bash
# Clear Unity cache
rm -rf ~/UnityProjects/NexoAgentDemo/Library/PackageCache
rm -f ~/UnityProjects/NexoAgentDemo/Packages/packages-lock.json

# Re-import packages
# Unity will automatically re-import on next launch
```

### **Interactive Demo Issues**
- **Non-interactive terminal**: Use `--demo` flag for automated mode
- **Missing dependencies**: Run `dotnet restore` first
- **Build errors**: Check .NET 8.0 SDK installation

### **OLLama Integration**
- **Service not running**: Start OLLama service first
- **Model missing**: Pull `llava:7b` model
- **Connection issues**: Check localhost:11434

---

## 🎯 **Next Steps**

1. **Explore Templates**: Try different project types
2. **Customize Tasks**: Create your own task definitions
3. **Integrate OLLama**: Set up visual analytics
4. **Extend Tools**: Add custom validation agents
5. **Scale Up**: Use for larger project management

---

## 📚 **Additional Resources**

- **Agent Foundry Documentation**: `docs/demos/AgentFoundry.md`
- **Unity Integration Guide**: `docs/demos/UnityAgentFoundry.md`
- **Visual Analytics Setup**: `docs/demos/VisualAnalytics.md`
- **API Reference**: `docs/api/`

---

**🎮 Ready to experience the future of AI-powered project management!**
