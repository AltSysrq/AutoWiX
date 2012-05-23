using System;
using System.IO;
using System.Xml;

public class Autowix {
  private static void printHelp() {
    System.Console.WriteLine("Usage: autowix infile");
  }

  private static int run(string infile) {
    return 0;
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