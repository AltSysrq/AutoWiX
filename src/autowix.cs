using System;
using System.IO;
using System.Xml;
using System.Collections.Generic;

public class Autowix {
  private static void printHelp() {
    System.Console.WriteLine("Usage: autowix infile");
  }

  private static int run(string infile) {
    string basename = Path.GetDirectoryName(infile) + "\\" +
      Path.GetFileNameWithoutExtension(infile);
    string persistenceFile = basename + ".awxg";
    string outfile = basename = ".wxs";

    XmlTextReader xin;
    XmlTextWriter xout;

    try {
      xin = new XmlTextReader(new FileStream(infile, FileMode.Open,
                                             FileAccess.Read));
    } catch (IOException e) {
      Console.WriteLine("Could not open " + infile + ": " + e.Message);
      return 1;
    }

    try {
      xout = new XmlTextWriter(new FileStream(outfile, FileMode.OpenOrCreate,
                                              FileAccess.Write),
                               System.Text.Encoding.UTF8);
    } catch (IOException e) {
      Console.WriteLine("Could not open " + outfile + ": " + e.Message);
      return 2;
    }

    Dictionary<string,string> persistence = readPersistenceFile(persistenceFile);

    transform(xin, xout, persistence);

    writePersistenceFile(persistenceFile, persistence);

    return 0;
  }

  private static Dictionary<string,string> readPersistenceFile(string infile) {
    return null;
  }

  private static void writePersistenceFile(string outfile,
                                           Dictionary<string,string> persistence) {
  }

  private static void transform(XmlTextReader xin, XmlTextWriter xout,
                                Dictionary<string,string> guids) {
  }

  static int Main(string[] args) {
    if (args.Length != 1 ||
        args[0] == "--help" ||
        args[0] == "-help" ||
        args[0] == "-?" ||
        args[0] == "/?") {
      printHelp();
      return 255;
    }

    return run(args[0]);
  }
}