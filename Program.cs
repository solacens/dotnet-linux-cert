using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using CommandLine;

namespace Linux.Cert {
  public static class Program {
    [Verb ("installCerts", HelpText = "Install .pfx certificates with .txt password file pair from the path specified")]
    internal class InstallCertsOptions {
      [Option ('f', "file", Required = true)]
      public string CertificatePath { get; set; }

      [Option ('p', "password", Required = true)]
      public string Password { get; set; }
    }

    [Verb ("autoInstallCerts", HelpText = "Install certificates pairs automattically from path specified, by default '/Certificates'")]
    internal class AutoInstallCertsOptions {
      [Option ('p', "path", Default = "/Certificates")]
      public string FolderPath { get; set; }
    }

    [Verb ("listCerts", HelpText = "List all certificates installed")]
    internal class ListCertsOptions {
      [Option ('n', "name", Default = "-")]
      public string StoreName { get; set; }

      [Option ('l', "location", Default = "-")]
      public string StoreLocation { get; set; }
    }

    private static void NotParsedFunc (IEnumerable<Error> arg) {
#if DEBUG
      throw new Exception (arg.First ().Tag.ToString ());
#endif
    }

    public static void Main (string[] args) {
      CommandLine.Parser.Default.ParseArguments<InstallCertsOptions, AutoInstallCertsOptions, ListCertsOptions> (args)
        .WithParsed<InstallCertsOptions> (options => InstallCertificate (options.CertificatePath, options.Password))
        .WithParsed<AutoInstallCertsOptions> (options => AutoInstallCertificates (options.FolderPath))
        .WithParsed<ListCertsOptions> (options => ListCertificates (options.StoreName, options.StoreLocation))
        .WithNotParsed (NotParsedFunc);
    }

    private static void InstallCertificate (string path, string password) {
      X509Certificate2 cert = null;
      if (!string.IsNullOrEmpty (path)) {
        Console.WriteLine ($"Reading certificate from '{path}'...");

        cert = new X509Certificate2 (
          path,
          password,
          X509KeyStorageFlags.DefaultKeySet);
      } else {
        throw new ArgumentNullException ("Unable to create certificate from provided arguments.");
      }

      var store = new X509Store (StoreName.My, StoreLocation.CurrentUser);
      store.Open (OpenFlags.ReadWrite);
      store.Add (cert);
      // Print thumbprint
      Console.WriteLine (cert.Thumbprint);
      store.Close ();
    }

    private static void AutoInstallCertificates (string folderPath) {
      Console.WriteLine ($"Auto installing certificates from [{folderPath}]");
      // If directory is not exists
      if (!Directory.Exists (folderPath)) {
          Console.WriteLine ($"Directory does not exists!\n----------------------------\nNumber of certificates installed: [0]\n----------------------------");
          return;
      }
      // Get pair of cert and password and install
      var certificateInstalled = 0;
      var files = new List<string> (Directory.EnumerateFiles (folderPath));
      foreach (var filePath in files) {
        var ext = Path.GetExtension (filePath);
        if (ext.Equals (".pfx")) {
          var passwordFilePath = Path.ChangeExtension (filePath, ".txt");
          if (files.Contains (passwordFilePath)) {
            var password = System.IO.File.ReadAllText (passwordFilePath);
            InstallCertificate (filePath, password);
            certificateInstalled++;
          }
        }
      }
      // Tell us how many of certs you've installed?
      Console.WriteLine ($"----------------------------\nNumber of certificates installed: [{certificateInstalled}]\n----------------------------");
    }

    private static void ListCertificates (string sName, string sLocation) {
      // See if we need to loop through all store
      var storeNameList = Enum.GetValues (typeof (StoreName)).Cast<StoreName> ().ToList ();
      var storeNameStringList = storeNameList.Select (x => x.ToString ()).ToList ();
      if (storeNameStringList.Contains (sName)) {
        storeNameList = new List<StoreName> {
          (StoreName) Enum.Parse (typeof (StoreName), sName)
        };
      }

      if (sLocation.Equals (StoreLocation.CurrentUser.ToString ()) || sLocation.Equals ("-")) {
        foreach (StoreName sname in storeNameList) {
          Console.WriteLine ($"----------------------------\nListing current user certificates in [{sname}]...");
          try {
            var storeCU = new X509Store (sname, StoreLocation.CurrentUser);
            storeCU.Open (OpenFlags.ReadOnly);
            var certificatesCU = storeCU.Certificates;
            foreach (var certificate in certificatesCU) {
              Console.WriteLine ($"  > {certificate.Thumbprint}\n    Name: {certificate.FriendlyName}\n    Subject: {certificate.Subject}\n    Issuer: {certificate.Issuer}");
            }
            storeCU.Close ();
        } catch (System.Security.Cryptography.CryptographicException e) {
            Console.WriteLine (e.ToString ());
          }
        }
      }

      if (sLocation.Equals (StoreLocation.LocalMachine.ToString ()) || sLocation.Equals ("-")) {
        foreach (StoreName sname in storeNameList) {
          Console.WriteLine ($"----------------------------\nListing local machine certificates in [{sname}]...");
          try {
            var storeLM = new X509Store (sname, StoreLocation.LocalMachine);
            storeLM.Open (OpenFlags.ReadOnly);
            var certificatesLM = storeLM.Certificates;
            foreach (var certificate in certificatesLM) {
              Console.WriteLine ($"  > {certificate.Thumbprint}\n    Name: {certificate.FriendlyName}\n    Subject: {certificate.Subject}\n    Issuer: {certificate.Issuer}");
            }
            storeLM.Close ();
          } catch (System.Security.Cryptography.CryptographicException e) {
            Console.WriteLine (e.ToString ());
          }
        }
      }

      Console.WriteLine ("----------------------------");
    }
  }
}
