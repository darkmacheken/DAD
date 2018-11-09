using System;
using System.Text.RegularExpressions;
using PuppetMaster.Exceptions;
using PuppetMaster.CommandStructure;

namespace PuppetMaster {
    public class Parser {
        public Script Parse(string[] lines) {
            Script script = new Script();
            Regex numRegex = new Regex("^[0-9]+$");

            for (int i=0; i < lines.Length; i++) {

                if (lines[i].StartsWith("\n") || lines[i].StartsWith("\r\n") || string.IsNullOrWhiteSpace(lines[i])) {
                    continue;
                }
                if (lines[i].StartsWith(" ") || lines[i].StartsWith("\t")) {
                    throw new IncorrectCommandException(i);
                }

                Regex exprRegex = new Regex("(\\n|\\r)+");
                string command = exprRegex.Replace(lines[i], string.Empty);

                string[] words = Regex.Split(command, "(\\s|\\t)+");

                if (words[0] == "Server") {
                    try {
                        string id = words[2];
                        Uri url = new Uri(words[4]);
                        int minDelay = Int32.Parse(words[6]);
                        int maxDelay = Int32.Parse(words[8]);
                        script.AddNode(new CreateServer(id, url, minDelay, maxDelay));
                    } catch (Exception ex) {
                        throw new IncorrectCommandException(i, ex.Message);
                    }

                } else if (words[0] == "Client") {
                    try {
                        string id = words[2];
                        Uri url = new Uri(words[4]);
                        string scriptFile = words[6];
                        script.AddNode(new CreateClient(id, url, scriptFile));
                    } catch (Exception ex) {
                        throw new IncorrectCommandException(i, ex.Message);
                    }

                } else if (words[0] == "Status") {
                    script.AddNode(new Status());

                } else if (words[0] == "Crash") {
                    string processName = words[2];
                    script.AddNode(new Crash(processName));
                
                } else if (words[0] == "Freeze") {
                    string processName = words[2];
                    script.AddNode(new Freeze(processName));
                
                } else if (words[0] == "Unfreeze") {
                    string processName = words[2];
                    script.AddNode(new Unfreeze(processName));

                } else if (words[0] == "Wait") {
                    try {
                        int time = Int32.Parse(words[2]);
                        script.AddNode(new Wait(time));
                    } catch (Exception ex) {
                        throw new IncorrectCommandException(i, ex.Message);
                    }

                } else {
                    throw new IncorrectCommandException(i);
                }
            }
            return script;
        }
    }
}