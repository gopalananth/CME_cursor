# Build Instructions for Chef Middle East Form Project

## Prerequisites
- Visual Studio 2019 or 2022
- .NET Framework 4.7.2 or higher
- NuGet Package Manager

## Step-by-Step Build Instructions

### 1. Clean Previous Build Artifacts
```cmd
# In Visual Studio Package Manager Console or Command Prompt in project root
rm -rf bin obj packages
# Or manually delete bin, obj, and packages folders
```

### 2. Restore NuGet Packages
```cmd
# Navigate to the solution directory
cd /path/to/CME_Cursor

# Restore packages for main project
nuget restore Chef_Middle_East_Form/packages.config -PackagesDirectory packages

# Restore packages for test project  
nuget restore Chef_Middle_East_Form.Tests/packages.config -PackagesDirectory packages
```

**Alternative in Visual Studio:**
- Right-click Solution → "Restore NuGet Packages"
- Build → "Rebuild Solution"

### 3. Verify Package Installation
Ensure these key packages are installed:
- **Main Project:** Aspose.Words, iText7, Newtonsoft.Json, Microsoft.AspNet.Mvc
- **Test Project:** MSTest.TestFramework, Moq, Castle.Core

### 4. Build Order
1. Build main project first: `Chef_Middle_East_Form`
2. Then build test project: `Chef_Middle_East_Form.Tests`

### 5. Common Issues and Solutions

#### Issue: "Assembly.cs missing in Properties"
**Solution:** Ensure the `Properties/AssemblyInfo.cs` file exists in both projects:
- Main project: `Chef_Middle_East_Form/Properties/AssemblyInfo.cs` ✓
- Test project: `Chef_Middle_East_Form.Tests/Properties/AssemblyInfo.cs` ✓

#### Issue: "Missing DLL references"
**Solution:** 
1. Clean solution completely
2. Delete `bin` and `obj` folders
3. Restore NuGet packages
4. Rebuild solution

#### Issue: "C# version syntax errors"
**Solution:** 
- Target Framework: .NET Framework 4.7.2
- C# Language Version: 7.3 (default for .NET Framework 4.7.2)
- Avoid using C# 8+ features (nullable reference types, pattern matching, etc.)

### 6. Visual Studio Configuration
1. **Target Framework:** .NET Framework 4.7.2
2. **C# Language Version:** 7.3 (explicitly set in project files)
3. **Configuration:** Debug or Release
4. **Platform:** Any CPU
5. **NuGet Package Format:** packages.config (traditional format)

### 7. Test Project Specific Notes
- Uses MSTest framework (not xUnit or NUnit)
- Moq 4.20.69 for mocking
- All async method names updated to match actual implementation:
  - `GetLeadDataAsync` → `GetLeadData`
  - `GetAccountDataAsync` → `GetAccountData`
  - `UpdateAccountAsync` → `UpdateAccount`
  - `CreateAccountAsync` → `CreateAccount`

### 8. Troubleshooting Commands
```cmd
# Clear NuGet cache
nuget locals all -clear

# Force package reinstall (in Package Manager Console)
Update-Package -reinstall

# Verify .NET Framework version
reg query "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" /v Release
```

### 9. File Structure Verification
Ensure all these files exist:
```
CME_Cursor/
├── Chef_Middle_East_Form/
│   ├── Properties/AssemblyInfo.cs ✓
│   ├── packages.config ✓
│   └── Chef_Middle_East_Form.csproj ✓
├── Chef_Middle_East_Form.Tests/
│   ├── Properties/AssemblyInfo.cs ✓ (FIXED)
│   ├── packages.config ✓ (UPDATED)
│   └── Chef_Middle_East_Form.Tests.csproj ✓ (FIXED)
└── OnlineCustomerPortal.sln ✓
```

### 10. Recent Fixes Applied
- ✅ **Create.cshtml formatting fixed** - Proper Razor syntax and CSS styling
- ✅ **C# 8.0 switch expressions converted** - Changed to C# 7.3 compatible switch statements
- ✅ **C# Language Version standardized** - Both projects explicitly use C# 7.3
- ✅ **Test project DLL references fixed** - All missing dependencies added
- ✅ **AssemblyInfo.cs created** - Test project now has proper assembly information

### 11. Success Indicators
- ✅ Solution builds without errors
- ✅ All NuGet packages restored
- ✅ Test project compiles successfully
- ✅ No missing assembly references
- ✅ Both projects use .NET Framework 4.7.2 with C# 7.3
- ✅ Create.cshtml displays properly without formatting issues
