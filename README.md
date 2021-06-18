# dotnet-linux-cert

> Helper for install and list PKCS#12 certificates for *nix system

## Installation

[Portal](https://www.nuget.org/packages/dotnet-linux-cert/)

## Usage

```sh
dotnet linux-cert --help

dotnet linux-cert install --help
dotnet linux-cert auto-install --help
dotnet linux-cert list --help
```

#### Folder structure required for `auto-install`
```sh
ls /Certifcates # Default reading directory or auto-install

a.pfx # Cert A, password protected
a.txt # Password or cert A
b.pfx # Cert B, non password protected
```

## Old version dotnet sdk support
Check [Changelog](CHANGELOG.md) to see which version needed to be installed
