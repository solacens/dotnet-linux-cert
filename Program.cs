using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System;
namespace Linux.Cert
{
  public static class Program
  {
    private const string PKCS12_FILE_EXTENSION = ".pfx";
    private const string PKCS12_PASSWORD_EXTENSION = ".txt";

    public static int Main(string[] args)
    {
      var root = new RootCommand("Dotnet linux cert helper: Install and list PKCS#12 certificates for *nix system");

      var installCommand = new Command("install", "Install certificates, optionally with password supplied")
      {
        new Option<string>(new string[] { "--path", "-f" }, "Path of the certificate")
        {
          IsRequired = true
        },
        new Option<string>(new string[] { "--password", "-p" }, "Password of the certificate"),
        new Option<string>(new string[] { "--store-name", "-n" }, () => "My", "Specify store name, available value: " +
          "[AddressBook AuthRoot CertificateAuthority Disallowed My Root TrustedPeople TrustedPublisher]"),
        new Option<string>(new string[] { "--store-location", "-l" }, () => "CurrentUser", "Specify store location, available value: [CurrentUser LocalMachine]")
      };

      var autoInstallCommand = new Command("auto-install", $"Auto install certificates (with {PKCS12_PASSWORD_EXTENSION} pairs) from path specified")
      {
        new Option<string>(new string[] { "--path", "-f" }, () => "/Certificates", "Path of the directory"),
        new Option<string>(new string[] { "--store-name", "-n" }, () => "My", "Specify store name, available value: " +
          "[AddressBook AuthRoot CertificateAuthority Disallowed My Root TrustedPeople TrustedPublisher]"),
        new Option<string>(new string[] { "--store-location", "-l" }, () => "CurrentUser", "Specify store location, available value: [CurrentUser LocalMachine]")
      };

      // TODO: Remove this
      // For backward compatibility
      var autoInstallDeprecatingCommand = new Command("autoInstallCerts", $"Auto install certificates (with {PKCS12_PASSWORD_EXTENSION} pairs) from path specified")
      {
        new Option<string>(new string[] { "--path", "-f" }, () => "/Certificates", "Path of the directory")
      };
      // Make this hidden
      autoInstallDeprecatingCommand.IsHidden = true;

      var listCommand = new Command("list", "List certificates, can be filtered by store name or location")
      {
        new Option<string>(new string[] { "--store-name", "-n" }, "Specify store name, available value: " +
          "[AddressBook AuthRoot CertificateAuthority Disallowed My Root TrustedPeople TrustedPublisher]"),
        new Option<string>(new string[] { "--store-location", "-l" }, "Specify store location, available value: [CurrentUser LocalMachine]")
      };

      installCommand.Handler = CommandHandler.Create<string, string, string, string>(InstallCertificate);
      autoInstallCommand.Handler = CommandHandler.Create<string, string, string>(AutoInstallCertificates);
      // TODO: Remove this
      autoInstallDeprecatingCommand.Handler = CommandHandler.Create<string>(AutoInstallCertificates);
      listCommand.Handler = CommandHandler.Create<string, string>(ListCertificates);

      root.Add(installCommand);
      root.Add(autoInstallCommand);
      // TODO: Remove this
      root.Add(autoInstallDeprecatingCommand);
      root.Add(listCommand);

      return root.InvokeAsync(args).Result;
    }

    private static void SepLog(string str)
    {
      var sep = new String('-', str.Length + 8);
      Console.WriteLine($"{sep}\n    {str}    \n{sep}");
    }

    private static List<StoreName> FilterStoreName(string str)
    {
      var storeNameList = Enum.GetValues(typeof(StoreName)).Cast<StoreName>().ToList();
      var storeNameStringList = storeNameList.Select(x => x.ToString()).ToList();
      if (storeNameStringList.Contains(str))
      {
        storeNameList = new List<StoreName> {
          (StoreName) Enum.Parse (typeof (StoreName), str)
        };
      }

      return storeNameList;
    }

    private static Boolean CheckStoreName(string str)
    {
      return FilterStoreName(str).Count == 1;
    }

    private static Boolean CheckStoreLocation(string str)
    {
      return str.Equals(StoreLocation.CurrentUser.ToString()) || str.Equals(StoreLocation.LocalMachine.ToString());
    }

    private static void InstallCertificate(string path, string password = null, string storeName = "My", string storeLocation = "CurrentUser")
    {
      X509Certificate2 cert = null;
      if (!string.IsNullOrEmpty(path))
      {
        Console.WriteLine($"Reading certificate from '{path}'...");

        if (!CheckStoreName(storeName))
        {
          Console.WriteLine($"ERROR: Specified store name [{storeName}] does not exists.");
          return;
        }

        if (!CheckStoreLocation(storeLocation))
        {
          Console.WriteLine($"ERROR: Specified store location [{storeLocation}] does not exists.");
          return;
        }

        try
        {
          if (password != null)
          {
            cert = new X509Certificate2(path, password);
          }
          else
          {
            cert = new X509Certificate2(path);
          }

          var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
          store.Open(OpenFlags.ReadWrite);
          store.Add(cert);
          Console.WriteLine($"Installed certificate thumbprint: {cert.Thumbprint}\n");
          store.Close();
        }
        catch (System.Security.Cryptography.CryptographicException e)
        {
          Console.WriteLine(e.ToString());
          Console.WriteLine("");
        }
      }
      else
      {
        throw new ArgumentNullException("Unable to create certificate from provided arguments.");
      }
    }

    private static void AutoInstallCertificates(string path, string storeName = "My", string storeLocation = "CurrentUser")
    {
      SepLog($"Auto installing certificates from [{path}]");
      // If directory is not exists
      if (!Directory.Exists(path))
      {
        Console.WriteLine($"ERROR: Directory does not exists");
        SepLog("Number of certificates installed: [0]");
        return;
      }
      // Get pair of cert and password and install
      var certificateInstalled = 0;
      var files = new List<string>(Directory.EnumerateFiles(path));
      foreach (var filePath in files)
      {
        var ext = Path.GetExtension(filePath);
        if (ext.Equals(PKCS12_FILE_EXTENSION))
        {
          var passwordFilePath = Path.ChangeExtension(filePath, PKCS12_PASSWORD_EXTENSION);
          if (files.Contains(passwordFilePath))
          {
            var password = System.IO.File.ReadAllText(passwordFilePath).Trim('\r', '\n');
            InstallCertificate(filePath, password, storeName, storeLocation);
            certificateInstalled++;
          }
          else
          {
            InstallCertificate(filePath, null, storeName, storeLocation);
            certificateInstalled++;
          }
        }
      }

      SepLog($"Number of certificates installed: [{certificateInstalled}]");
    }

    // TODO: Remove this
    // For deprecating call
    private static void AutoInstallCertificates(string path)
    {
      AutoInstallCertificates(path, "My", "CurrentUser");
    }

    private static void ListCertificates(string storeName, string storeLocation = "-")
    {
      // See if we need to loop through all store name
      var storeNameList = FilterStoreName(storeName);

      if (storeLocation.Equals(StoreLocation.CurrentUser.ToString()) || storeLocation.Equals("-"))
      {
        foreach (StoreName sname in storeNameList)
        {
          SepLog($"Listing current user certificates in [{sname}]...");
          try
          {
            var storeCU = new X509Store(sname, StoreLocation.CurrentUser);
            storeCU.Open(OpenFlags.ReadOnly);
            var certificatesCU = storeCU.Certificates;
            foreach (var certificate in certificatesCU)
            {
              Console.WriteLine($"  > {certificate.Thumbprint}\n    Name: {certificate.FriendlyName}\n    Subject: {certificate.Subject}\n    Issuer: {certificate.Issuer}");
            }
            storeCU.Close();
          }
          catch (System.Security.Cryptography.CryptographicException e)
          {
            Console.WriteLine(e.ToString());
          }
        }
      }

      if (storeLocation.Equals(StoreLocation.LocalMachine.ToString()) || storeLocation.Equals("-"))
      {
        foreach (StoreName sname in storeNameList)
        {
          SepLog($"Listing local machine certificates in [{sname}]...");
          try
          {
            var storeLM = new X509Store(sname, StoreLocation.LocalMachine);
            storeLM.Open(OpenFlags.ReadOnly);
            var certificatesLM = storeLM.Certificates;
            foreach (var certificate in certificatesLM)
            {
              Console.WriteLine($"  > {certificate.Thumbprint}\n    Name: {certificate.FriendlyName}\n    Subject: {certificate.Subject}\n    Issuer: {certificate.Issuer}");
            }
            storeLM.Close();
          }
          catch (System.Security.Cryptography.CryptographicException e)
          {
            Console.WriteLine(e.ToString());
          }
        }
      }
    }
  }
}
