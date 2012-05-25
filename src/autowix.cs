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
    xout.Flush();
    xout.Close();

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
          translateGUIDs(attr, guids, xin);

          //Write new node(s)
          if (name == "autowixfilecomponents")
            writeFileComponents(xout, name, attr, empty, guids, xin);
          else
            writeVerbatim(xout, name, attr, empty);
        } else if (xin.NodeType == XmlNodeType.EndElement) {
          xout.WriteEndElement();
        }
      }
    } catch (XmlException e) {
      Console.WriteLine("Error reading input: " + e.Message);
      Environment.Exit(5);
    }
  }

  //Translates any GUID-key attributes to their actual GUIDs.
  private static void translateGUIDs(KeyValuePair<string,string>[] attrs,
                                     Dictionary<string,string> guids,
                                     XmlTextReader xin) {
    for (int i = 0; i < attrs.Length; ++i) {
      KeyValuePair<string,string> attr = attrs[i];
      if (attr.Value.StartsWith("autowix:guid:")) {
        //Disallow line breaks in GUID identifiers
        if (attr.Value.Contains("\n") || attr.Value.Contains("\r")) {
          Console.WriteLine("Error: Line {0}: GUID id containing a line break",
                            xin.LineNumber);
          Environment.Exit(6);
        }
        //Spaces cannot be used in GUID identifiers.
        //If any are found, replace with underscores and issue a warning.
        if (attr.Value.Contains(" ")) {
          Console.WriteLine("Warning: {0}: GUID id may not contain spaces.",
                            xin.LineNumber);
          Console.WriteLine("Warning: Converting spaces to underscores.");
          string newValue = attr.Value.Replace(' ', '_');
          Console.WriteLine("Warning: {0} => {1}", attr.Value, newValue);

          attr = attrs[i] = new KeyValuePair<string,string>(attr.Key, newValue);
        }

        //This attribute references an autogenerated GUID.
        attrs[i] = new KeyValuePair<string, string>(attr.Key,
                                                    translateGUID(attr.Value,
                                                                  guids));
      }
    }
  }

  //Translates (and creates if needed) a GUID with the given key.
  //No validation is performed.
  private static string translateGUID(string key,
                                      Dictionary<string,string> guids) {
    //If no GUID with this key exists, generate one now.
    //Then, replace the value with that GUID
    if (!guids.ContainsKey(key)) {
      guids[key] = Guid.NewGuid().ToString().ToUpper();
    }

    return guids[key];
  }

  private static void writeFileComponents(XmlTextWriter xout, string name,
                                          KeyValuePair<string,string>[] attrs,
                                          bool empty,
                                          Dictionary<string,string> guids,
                                          XmlTextReader xin) {
    if (!empty) {
      Console.WriteLine("Error: Line {0}: <autowixfilecomponents> not empty.",
                        xin.LineNumber);
      Environment.Exit(7);
    }

    string idbase = "autowix", dirbase = null, dirroot = null;
    foreach (KeyValuePair<string,string> attr in attrs) {
      if (attr.Key == "idbase")
        idbase = attr.Value;
      else if (attr.Key == "base")
        dirbase = attr.Value;
      else if (attr.Key == "from")
        dirroot = attr.Value;
      else {
        Console.WriteLine("Warning: Line {0}: Unknown attribute: {1}",
                          xin.LineNumber, attr.Key);
      }
    }

    if (dirroot == null) {
      Console.WriteLine("Error: Line {0}: Missing \"from\" attribute.",
                        xin.LineNumber);
      Environment.Exit(7);
    }

    //Create directories specified in base (if present)
    string accum = "";
    if (dirbase != null) {
      foreach (string subdir in dirbase.Split('\\')) {
        accum += "\\" + subdir;
        xout.WriteStartElement("Directory");
        xout.WriteAttributeString("Id", idbase + ":dir:" + accum);
        xout.WriteAttributeString("Name", subdir);
      }
    }

    //Move to root
    string[] rootdirs = dirroot.Split('\\');
    for (int i = 0; i < rootdirs.Length-1; ++i) {
      accum += "\\" + rootdirs[i];
      xout.WriteStartElement("Directory");
      xout.WriteAttributeString("Id", idbase + ":dir:" + accum);
      xout.WriteAttributeString("Name", rootdirs[i]);
    }

    //Traverse
    writeSingleFileComponent(xout, idbase, accum, dirroot, guids);

    //Close opened directories
    for (int i = 0; i < rootdirs.Length-1; ++i)
      xout.WriteEndElement();
    if (dirbase != null)
      for (int i = 0; i < dirbase.Split().Length; ++i)
        xout.WriteEndElement();
  }

  private static void writeSingleFileComponent(XmlTextWriter xout,
                                               string idbase, string accumPath,
                                               string file,
                                               Dictionary<string,string> guids)
  {
    string basename = Path.GetFileName(file);
    Console.WriteLine(file);
    accumPath += "\\" + basename;
    if (File.Exists(file)) {
      //Create component
      xout.WriteStartElement("Component");
      xout.WriteAttributeString("Id", idbase + ":comp:" + accumPath);
      xout.WriteAttributeString("Guid",
                                translateGUID(accumPath.Replace(' ','*'),
                                              guids));
      xout.WriteStartElement("File");
      xout.WriteAttributeString("Id", idbase + ":file:" + accumPath);
      xout.WriteAttributeString("Name", basename);
      xout.WriteAttributeString("DiskId", "1");
      xout.WriteAttributeString("Source", file);
      xout.WriteAttributeString("KeyPath", "yes");
      xout.WriteEndElement();
      xout.WriteEndElement();
    } else if (Directory.Exists(file)) {
      //Create directory
      xout.WriteStartElement("Directory");
      xout.WriteAttributeString("Id", idbase + ":dir:" + accumPath);
      xout.WriteAttributeString("Name", basename);

      //Recurse
      foreach (string sub in Directory.GetFileSystemEntries(file))
        writeSingleFileComponent(xout, idbase, accumPath, sub, guids);

      //End directory
      xout.WriteEndElement();
    } else {
      Console.WriteLine("Error: File \"{0}\" not found.", file);
      Environment.Exit(8);
    }
  }

  private static void writeVerbatim(XmlTextWriter xout, string name,
                                    KeyValuePair<string,string>[] attrs,
                                    bool empty) {
    xout.WriteStartElement(name);
    foreach (KeyValuePair<string,string> attr in attrs)
      xout.WriteAttributeString(attr.Key, attr.Value);
    if (empty)
      xout.WriteEndElement();
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