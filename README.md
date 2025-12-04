# ios-adhoc-ota

[![Build](https://github.com/aherrick/ios-adhoc-ota/actions/workflows/build.yml/badge.svg)](https://github.com/aherrick/ios-adhoc-ota/actions/workflows/build.yml)

A GitHub Action that uploads iOS IPA files to Azure Blob Storage with automatic OTA (Over-The-Air) installation support.

> ⚠️ **Ad-Hoc Distribution:** This is for ad-hoc iOS builds, which require each test device's UUID to be registered in your provisioning profile before building. [Learn more about ad-hoc distribution](https://developer.apple.com/documentation/xcode/distributing-your-app-to-registered-devices)

## Prerequisites

Before using this action, you need:

1. **Azure Storage Account** - Create a storage account in the Azure Portal
2. **Static Website Hosting Enabled** - In your storage account settings, enable "Static website" hosting. This automatically creates the `$web` container used to serve your files publicly
3. **Ad-Hoc Signed IPA** - Your iOS app must be signed with an ad-hoc provisioning profile that includes the UDIDs of all test devices

## What It Does

- Uploads your IPA to Azure Blob Storage's static website (`$web` container)
- Generates an Apple-compatible `manifest.plist` for OTA installation
- Creates a simple landing page with an "Install App" button
- Works with enterprise/ad-hoc signed apps for internal distribution

## Usage

```yaml
- uses: aherrick/ios-adhoc-ota@main
  with:
    app_name: 'My App'
    bundle_id: 'com.example.myapp'
    version: '1.0.0'
    ipa_name: 'MyApp.ipa'
    ipa_path: 'build/MyApp.ipa'
    static_url: 'https://myaccount.z13.web.core.windows.net'
    azure_storage_account: ${{ secrets.AZURE_STORAGE_ACCOUNT }}
    azure_storage_key: ${{ secrets.AZURE_STORAGE_KEY }}
```

## Inputs

| Input | Description | Required |
|-------|-------------|----------|
| `app_name` | Display name of your app | Yes |
| `bundle_id` | iOS bundle identifier (e.g., `com.company.app`) | Yes |
| `version` | App version string | Yes |
| `ipa_name` | Name of the IPA file | Yes |
| `ipa_path` | Path to IPA file relative to workspace | Yes |
| `static_url` | Base URL of your Azure static website | Yes |
| `azure_storage_account` | Azure Storage account name | Yes |
| `azure_storage_key` | Azure Storage account key | Yes |

## Outputs

| Output | Description |
|--------|-------------|
| `landing_page_url` | URL to the install landing page |
| `ipa_url` | Direct URL to the IPA file |
| `manifest_url` | URL to the manifest.plist |

## Azure Setup

1. Create an Azure Storage account
2. Enable **Static website** hosting in the storage account settings
3. Note your primary endpoint URL (e.g., `https://myaccount.z13.web.core.windows.net`)
4. Add your storage account name and key as GitHub secrets

## How OTA Installation Works

iOS allows enterprise and ad-hoc apps to be installed via a special `itms-services://` URL that points to a manifest.plist file. This action generates that manifest and a landing page that users can open on their iOS device to install the app.

> **Note:** The IPA must be signed with an enterprise or ad-hoc provisioning profile. The device must trust the signing certificate.

## License

MIT
