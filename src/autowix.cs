using System;
using System.IO;
using System.Xml;
using System.Collections.Generic;

public class Autowix {
  private static void printHelp() {
    System.Console.WriteLine("Usage: autowix infile");
  }

  private static int run(string infile) {
    infile = Path.GetFullPath(infile);
    string basename = Path.GetDirectoryName(infile) + "\\" +
      Path.GetFileNameWithoutExtension(infile);
    string persistenceFile = basename + ".awxg";
    string outfile = basename + ".wxs";

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

    Dictionary<string,string> persistence=readPersistenceFile(persistenceFile);

    transform(xin, xout, persistence);

    writePersistenceFile(persistenceFile, persistence);

    return 0;
  }

  private static Dictionary<string,string> readPersistenceFile(string infile) {
    Dictionary<string, string> map = new Dictionary<string, string>();

    try {
      int lineNum = 0;
      using (TextReader tin = new StreamReader(
                 new FileStream(infile, FileMode.Open, FileAccess.Read))) {
        string line;
        while (null != (line = tin.ReadLine())) {
          ++lineNum;
          string[] parts = line.Split(new char[] { ' ' });
          if (parts.Length != 2) {
            Console.WriteLine("Error: " + infile + ":" +
                              lineNum + ": malformed input");
            Environment.Exit(3);
          }

          map[parts[0]] = parts[1];
        }
      }
    } catch (FileNotFoundException e) {
      Console.WriteLine("Note: " + infile + " does not exist.");
      Console.WriteLine("Note: All GUIDs will be newly generated.");
    } catch (IOException e) {
      Console.WriteLine("Error: Reading " + infile + ": " + e.Message);
      Environment.Exit(3);
    }

    return map;
  }

  private static void writePersistenceFile(string outfile,
                                           Dictionary<string,string> map) {
    try {
      using (TextWriter tout = new StreamWriter(
             new FileStream(outfile, FileMode.OpenOrCreate,
                            FileAccess.Write))) {
        foreach (KeyValuePair<string,string> kv in map) {
          tout.WriteLine(kv.Key + " " + kv.Value);
        }
      }
    } catch (IOException e) {
      Console.WriteLine("Error: Could not write to " + outfile +
                        ": " + e.Message);
      Console.WriteLine("Generated GUIDs have NOT been saved!");
    }
  }

  private static void transform(XmlTextReader xin, XmlTextWriter xout,
                                Dictionary<string,string> guids) {
    try {
      while (xin.Read()) {
        if (xin.NodeType == XmlNodeType.Element) {
          string name = xin.Name;
          bool empty = xin.IsEmptyElement;
          //Extract attributes
          KeyValuePair<string,string>[] attr =
            new KeyValuePair<string,string>[xin.HasAttributes?
                                            xin.AttributeCount : 0];
          for (int i = 0; i < attr.Length; ++i) {
            xin.MoveToAttribute(i);
            attr[i] = new KeyValuePair<string,string>(xin.Name, xin.Value);
          }

          //Perform GUID translation
          translateGUIDs(attr, guids);

          //Write new node(s)
          if (name == "autowixfilecomponents")
            writeFileComponents(name, attr, empty, guids);
          else
            writeVerbatim(name, attr, empty);
        } else if (xin.NodeType == XmlNodeType.EndElement) {
          xout.WriteEndElement();
        }
      }
    } catch (XmlException e) {
      // TODO
    }
  }

  private static void translateGUIDs(KeyValuePair<string,string>[] attrs,
                                     Dictionary<string,string> guids) {
    for (int i = 0; i < attrs.Length; ++i) {
      KeyValuePair<string,string> attr = attrs[i];
      if (attr.Value.StartsWith("autowix:guid:")) {
        //This attribute references an autogenerated GUID.
        //If no GUID with this key exists, generate one now.
        //Then, replace the value with that GUID
        if (!guids.ContainsKey(attr.Value)) {
          guids[attr.Value] = Guid.NewGuid().ToString().ToUpper();
        }

        attrs[i] = new KeyValuePair<string,string>(attr.Key,guids[attr.Value]);
      }
    }
  }

  private static void writeFileComponents(string name,
                                          KeyValuePair<string,string>[] attrs,
                                          bool empty,
                                          Dictionary<string,string> guids) {
    // TODO
  }

  private static void writeVerbatim(string name,
                                    KeyValuePair<string,string>[] attrs,
                                    bool empty) {
    // TODO
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