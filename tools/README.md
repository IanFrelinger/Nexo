# Nexo Framework Development Tools

This directory contains tools to help with Nexo framework development and progress tracking.

## Progress Update Script

The `update-progress.sh` script automatically updates the `PROJECT_TRACKING.md` file with new progress entries.

### Usage

```bash
./tools/update-progress.sh "Category" "Description" [--section "SectionName"]
```

### Examples

```bash
# Add a feature implementation to General Notes
./tools/update-progress.sh "Feature Implementation" "Completed resource optimization service"

# Add a bug fix to Phase 0 Notes
./tools/update-progress.sh "Bug Fixes" "Fixed compilation errors in Infrastructure tests" --section "Phase 0 Notes"

# Add testing progress
./tools/update-progress.sh "Testing" "Added comprehensive tests for ResourceOptimizer"
```

### Categories

- **Feature Implementation**: New features or capabilities
- **Bug Fixes**: Error resolutions and improvements
- **Testing**: Test coverage and validation
- **Documentation**: Code comments, README updates, architecture docs
- **Refactoring**: Code improvements and optimizations
- **Build System**: Build configuration and dependency updates
- **Performance**: Performance improvements and optimizations

### Sections

- **General Notes** (default)
- **Phase 0 Notes**
- **Phase 1 Notes**
- **Phase 2 Notes**
- **Phase 3 Notes**

### Features

- ✅ Validates categories and sections
- ✅ Automatically updates last modified date
- ✅ Provides helpful error messages
- ✅ Suggests git commit commands
- ✅ Color-coded output for better readability

## IDE System Prompt

The `IDE_SYSTEM_PROMPT.md` file contains a comprehensive system prompt for your IDE's chat model (like Cursor).

### How to Use

1. Copy the content from `IDE_SYSTEM_PROMPT.md`
2. Paste it into your IDE's system prompt settings
3. The AI will now:
   - Monitor context window usage
   - Offer to summarize when approaching 80% limit
   - Automatically suggest progress document updates
   - Maintain focus on Nexo framework development

### Key Features

- **Context Window Management**: Proactively manages conversation length
- **Progress Tracking**: Automatically suggests updates to PROJECT_TRACKING.md
- **Nexo-Specific**: Tailored for the Nexo framework architecture
- **Development Workflow**: Integrates with your development process

## Workflow Integration

### Recommended Workflow

1. **Start Development Session**:
   ```bash
   # Check current progress
   cat PROJECT_TRACKING.md | grep -A 10 "### General Notes"
   ```

2. **During Development**:
   - Use the IDE chat with the system prompt
   - Let the AI suggest progress updates
   - Use the update script for manual entries

3. **Complete Work**:
   ```bash
   # Update progress
   ./tools/update-progress.sh "Feature Implementation" "Completed new feature"
   
   # Commit changes
   git add PROJECT_TRACKING.md
   git commit -m "Update progress: Feature Implementation - Completed new feature"
   ```

4. **Context Management**:
   - When the AI offers to summarize, accept it
   - Start a new conversation with the summary
   - Continue development with fresh context

### Context Summarization

When the AI offers to summarize (at 80% context usage), it will provide:

- Key decisions made
- Completed work
- Current status
- Progress document updates needed

This helps maintain continuity between conversations while managing context limits.

## Troubleshooting

### Script Issues

```bash
# Check script permissions
ls -la tools/update-progress.sh

# Make executable if needed
chmod +x tools/update-progress.sh

# Test with help
./tools/update-progress.sh --help
```

### Progress File Issues

```bash
# Check if progress file exists
ls -la PROJECT_TRACKING.md

# Backup before manual edits
cp PROJECT_TRACKING.md PROJECT_TRACKING.md.backup
```

### IDE Integration Issues

- Ensure your IDE supports custom system prompts
- Check that the prompt is properly formatted for your IDE
- Verify that the AI is following the context management instructions

## Best Practices

1. **Regular Updates**: Update progress after completing significant work
2. **Consistent Categories**: Use the predefined categories for consistency
3. **Context Management**: Accept summarization offers to maintain efficiency
4. **Documentation**: Keep the progress document up to date
5. **Version Control**: Commit progress updates regularly

## Contributing

To add new tools or improve existing ones:

1. Create new scripts in the `tools/` directory
2. Update this README with usage instructions
3. Test thoroughly before committing
4. Follow the existing patterns and conventions 