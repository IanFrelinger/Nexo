#!/bin/bash

# Progress Document Update Script for Nexo Framework
# Usage: ./tools/update-progress.sh "Category" "Description" [--section "SectionName"]

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Default values
PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PROGRESS_FILE="$PROJECT_ROOT/PROJECT_TRACKING.md"
CATEGORY=""
DESCRIPTION=""
SECTION="General Notes"

# Function to print usage
print_usage() {
    echo -e "${BLUE}Usage:${NC} $0 \"Category\" \"Description\" [--section \"SectionName\"]"
    echo ""
    echo -e "${BLUE}Examples:${NC}"
    echo "  $0 \"Feature Implementation\" \"Completed resource optimization service\""
    echo "  $0 \"Bug Fixes\" \"Fixed compilation errors in Infrastructure tests\" --section \"Phase 0 Notes\""
    echo "  $0 \"Testing\" \"Added comprehensive tests for ResourceOptimizer\""
    echo ""
    echo -e "${BLUE}Categories:${NC}"
    echo "  - Feature Implementation"
    echo "  - Bug Fixes"
    echo "  - Testing"
    echo "  - Documentation"
    echo "  - Refactoring"
    echo "  - Build System"
    echo "  - Performance"
    echo ""
    echo -e "${BLUE}Sections:${NC}"
    echo "  - General Notes (default)"
    echo "  - Phase 0 Notes"
    echo "  - Phase 1 Notes"
    echo "  - Phase 2 Notes"
    echo "  - Phase 3 Notes"
}

# Function to validate category
validate_category() {
    local valid_categories=(
        "Feature Implementation"
        "Bug Fixes"
        "Testing"
        "Documentation"
        "Refactoring"
        "Build System"
        "Performance"
    )
    
    for cat in "${valid_categories[@]}"; do
        if [[ "$1" == "$cat" ]]; then
            return 0
        fi
    done
    
    echo -e "${RED}Error: Invalid category '$1'${NC}"
    echo -e "${YELLOW}Valid categories:${NC}"
    printf '  - %s\n' "${valid_categories[@]}"
    return 1
}

# Function to validate section
validate_section() {
    local valid_sections=(
        "General Notes"
        "Phase 0 Notes"
        "Phase 1 Notes"
        "Phase 2 Notes"
        "Phase 3 Notes"
    )
    
    for sec in "${valid_sections[@]}"; do
        if [[ "$1" == "$sec" ]]; then
            return 0
        fi
    done
    
    echo -e "${RED}Error: Invalid section '$1'${NC}"
    echo -e "${YELLOW}Valid sections:${NC}"
    printf '  - %s\n' "${valid_sections[@]}"
    return 1
}

# Function to find section line number
find_section_line() {
    local section="$1"
    local line_num=0
    
    while IFS= read -r line; do
        ((line_num++))
        if [[ "$line" == "### $section" ]]; then
            echo $line_num
            return 0
        fi
    done < "$PROGRESS_FILE"
    
    return 1
}

# Function to find insertion point in section
find_insertion_point() {
    local section_line="$1"
    local line_num="$section_line"
    local max_lines=$(wc -l < "$PROGRESS_FILE")
    
    # Skip the section header
    ((line_num++))
    
    # Find the end of the section (next ### or end of file)
    while ((line_num <= max_lines)); do
        local line=$(sed -n "${line_num}p" "$PROGRESS_FILE")
        
        # If we hit another section header, we're done
        if [[ "$line" =~ ^### ]]; then
            break
        fi
        
        # If we hit the end of the notes section, we're done
        if [[ "$line" == "- [ ] Add notes here as needed" ]]; then
            break
        fi
        
        ((line_num++))
    done
    
    echo $line_num
}

# Function to update progress file
update_progress_file() {
    local category="$1"
    local description="$2"
    local section="$3"
    
    echo -e "${BLUE}Updating progress document...${NC}"
    echo -e "${YELLOW}Category:${NC} $category"
    echo -e "${YELLOW}Description:${NC} $description"
    echo -e "${YELLOW}Section:${NC} $section"
    
    # Find the section
    local section_line=$(find_section_line "$section")
    if [[ $? -ne 0 ]]; then
        echo -e "${RED}Error: Section '$section' not found in $PROGRESS_FILE${NC}"
        return 1
    fi
    
    # Find insertion point
    local insert_line=$(find_insertion_point "$section_line")
    
    # Create the new entry
    local new_entry="- [x] **$category**: $description"
    
    # Insert the new entry
    sed -i.bak "${insert_line}i\\
$new_entry" "$PROGRESS_FILE"
    
    # Remove backup file
    rm "${PROGRESS_FILE}.bak"
    
    echo -e "${GREEN}✓ Progress document updated successfully!${NC}"
    echo -e "${BLUE}Entry added at line $insert_line${NC}"
}

# Function to update last modified date
update_last_modified() {
    local current_date=$(date +"%B %d, %Y")
    local next_review=$(date -d "+2 weeks" +"%B %d, %Y" 2>/dev/null || date -v+2w +"%B %d, %Y" 2>/dev/null || echo "January 9, 2025")
    
    # Update the last modified date at the bottom of the file
    sed -i.bak "s/\*Last Updated: .*\*/\*Last Updated: $current_date\*/" "$PROGRESS_FILE"
    sed -i.bak "s/\*Next Review: .*\*/\*Next Review: $next_review\*/" "$PROGRESS_FILE"
    
    # Remove backup file
    rm "${PROGRESS_FILE}.bak"
    
    echo -e "${GREEN}✓ Last modified date updated to $current_date${NC}"
}

# Main script logic
main() {
    # Check for help flag first
    if [[ $# -eq 1 && ("$1" == "--help" || "$1" == "-h") ]]; then
        print_usage
        exit 0
    fi
    
    # Check if we have the required arguments
    if [[ $# -lt 2 ]]; then
        echo -e "${RED}Error: Missing required arguments${NC}"
        print_usage
        exit 1
    fi
    
    # Parse arguments
    CATEGORY="$1"
    DESCRIPTION="$2"
    shift 2
    
    # Parse optional arguments
    while [[ $# -gt 0 ]]; do
        case $1 in
            --section)
                SECTION="$2"
                shift 2
                ;;
            --help|-h)
                print_usage
                exit 0
                ;;
            *)
                echo -e "${RED}Error: Unknown option $1${NC}"
                print_usage
                exit 1
                ;;
        esac
    done
    
    # Validate inputs
    if ! validate_category "$CATEGORY"; then
        exit 1
    fi
    
    if ! validate_section "$SECTION"; then
        exit 1
    fi
    
    # Check if progress file exists
    if [[ ! -f "$PROGRESS_FILE" ]]; then
        echo -e "${RED}Error: Progress file not found at $PROGRESS_FILE${NC}"
        exit 1
    fi
    
    # Update the progress file
    if update_progress_file "$CATEGORY" "$DESCRIPTION" "$SECTION"; then
        # Update last modified date
        update_last_modified
        
        echo ""
        echo -e "${GREEN}Progress document update completed successfully!${NC}"
        echo -e "${BLUE}You can now commit these changes with:${NC}"
        echo "  git add PROJECT_TRACKING.md"
        echo "  git commit -m \"Update progress: $CATEGORY - $DESCRIPTION\""
    else
        echo -e "${RED}Failed to update progress document${NC}"
        exit 1
    fi
}

# Run main function with all arguments
main "$@" 