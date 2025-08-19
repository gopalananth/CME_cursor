# PowerShell script to convert AIML_MRRM_Prompts.docx to Markdown format
# This script uses .NET libraries to extract content from .docx files

param(
    [string]$InputFile = "AIML_MRRM_Prompts.docx",
    [string]$OutputFile = "AIML_MRRM_Prompts.md"
)

function Convert-DocxToMarkdown {
    param(
        [string]$DocxPath,
        [string]$OutputPath
    )
    
    try {
        # Check if input file exists
        if (-not (Test-Path $DocxPath)) {
            Write-Error "Input file not found: $DocxPath"
            return $false
        }
        
        Write-Host "Converting $DocxPath to Markdown..."
        
        # Load the .NET assembly for working with Open XML documents
        Add-Type -AssemblyName System.IO.Packaging
        
        # Create a temporary directory for extraction
        $tempDir = Join-Path $env:TEMP "docx_extract_$(Get-Random)"
        New-Item -ItemType Directory -Path $tempDir -Force | Out-Null
        
        try {
            # Extract the .docx file (it's essentially a ZIP file)
            Expand-Archive -Path $DocxPath -DestinationPath $tempDir -Force
            
            # Read the document.xml file which contains the main content
            $documentXmlPath = Join-Path $tempDir "word\document.xml"
            
            if (Test-Path $documentXmlPath) {
                $xmlContent = Get-Content $documentXmlPath -Raw -Encoding UTF8
                
                # Simple XML parsing to extract text content
                $markdownContent = @()
                
                # Extract text from paragraphs
                $paragraphs = [regex]::Matches($xmlContent, '<w:p[^>]*>(.*?)</w:p>', [System.Text.RegularExpressions.RegexOptions]::Singleline)
                
                foreach ($paragraph in $paragraphs) {
                    $paraContent = $paragraph.Groups[1].Value
                    
                    # Check if this is a heading (look for heading styles)
                    if ($paraContent -match 'w:val="Heading(\d+)"') {
                        $level = $matches[1]
                        $text = Extract-TextFromXml $paraContent
                        if ($text.Trim()) {
                            $markdownContent += "#" * $level + " " + $text.Trim()
                        }
                    } else {
                        $text = Extract-TextFromXml $paraContent
                        if ($text.Trim()) {
                            $markdownContent += $text.Trim()
                        } else {
                            $markdownContent += ""
                        }
                    }
                }
                
                # Write to markdown file
                $markdownContent | Out-File -FilePath $OutputPath -Encoding UTF8
                
                Write-Host "Successfully converted to: $OutputPath"
                return $true
            } else {
                Write-Error "Could not find document.xml in the .docx file"
                return $false
            }
        } finally {
            # Clean up temporary directory
            if (Test-Path $tempDir) {
                Remove-Item -Path $tempDir -Recurse -Force
            }
        }
    } catch {
        Write-Error "Error during conversion: $_"
        return $false
    }
}

function Extract-TextFromXml {
    param([string]$XmlContent)
    
    # Extract text from w:t elements
    $textElements = [regex]::Matches($XmlContent, '<w:t[^>]*>(.*?)</w:t>')
    $text = ""
    
    foreach ($element in $textElements) {
        $text += $element.Groups[1].Value
    }
    
    # Decode XML entities
    $text = $text -replace '&amp;', '&'
    $text = $text -replace '&lt;', '<'
    $text = $text -replace '&gt;', '>'
    $text = $text -replace '&quot;', '"'
    $text = $text -replace '&#39;', "'"
    
    return $text
}

# Main execution
Write-Host "Docx to Markdown Converter"
Write-Host "=========================="

$success = Convert-DocxToMarkdown -DocxPath $InputFile -OutputPath $OutputFile

if ($success) {
    Write-Host "`nConversion completed successfully!"
    Write-Host "You can now open $OutputFile in your text editor."
} else {
    Write-Host "`nConversion failed. Please check the error messages above."
}
