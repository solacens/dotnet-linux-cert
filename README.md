# dotnet-linux-cert

> Helper for install and list PKCS#12 certificates for *nix system

## Installation

[Portal](https://www.nuget.org/packages/dotnet-linux-cert/)

## Usage

Detail explanation in the help commands

```sh
dotnet linux-cert --help

dotnet linux-cert install --help
dotnet linux-cert auto-install --help
dotnet linux-cert list --help
```

#### Folder structure required for `auto-install`
```sh
ls /Certifcates # Default reading directory for auto-install

a.pfx # Cert A, password protected
a.txt # Password for cert A
b.pfx # Cert B, non password protected
```

#### Nuget publish
```sh
dotnet pack -c Release -o NugetOutput
```

#### Self-contained deployment releases
```sh
FRAMEWORK=net7.0
RID=linux-musl-x64
dotnet publish -c Release -o SCDOutput/$RID -f $FRAMEWORK -r $RID --self-contained
```

## Changelog
[Changelog](CHANGELOG.md)
