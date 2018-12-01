using System;
using System.Linq;
using System.Text.RegularExpressions;
using PuppetMaster.CommandStructure;
using PuppetMaster.Exceptions;

namespace PuppetMaster {
    public static class Parser {
        public static Script Parse(string command) {
            return Parse(new[] { command });
        }

        public static Script Parse(string[] lines) {
            Script script = new Script();

            for (int i = 0; i < lines.Length; i++) {

                if (lines[i].StartsWith("\n") || lines[i].StartsWith("\r\n") || string.IsNullOrWhiteSpace(lines[i])) {
                    continue;
                }
                if (lines[i].StartsWith(" ") || lines[i].StartsWith("\t")) {
                    throw new IncorrectCommandException(i);
                }

                Regex exprRegex = new Regex("(\\n|\\r)+");
                string command = exprRegex.Replace(lines[i], string.Empty);

                string[] words = Regex.Split(command, "(\\s|\\t)+")
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToArray();

                string expectedMessage = "Unexpected Command";
                try {
                    switch (words[0]) {
                        case "Server": {
                            expectedMessage =
                                $"{Environment.NewLine}Expected: Server server_id URL min_delay max_delay protocol";

                            if (words.Length != 6) {
                                throw new IncorrectCommandException(i, expectedMessage);
                            }

                            string id = words[1];
                            Uri url = new Uri(words[2]);
                            int minDelay = int.Parse(words[3]);
                            int maxDelay = int.Parse(words[4]);
                            string protocol = words[5];
                            script.AddNode(new CreateServer(id, url, minDelay, maxDelay, protocol));
                            break;
                        }
                        case "Client": {
                            expectedMessage = $"{Environment.NewLine}Expected: Client client_id URL script_file";

                            if (words.Length != 4) {
                                throw new IncorrectCommandException(i, expectedMessage);
                            }

                            string id = words[1];
                            Uri url = new Uri(words[2]);
                            string scriptFile = words[3];
                            script.AddNode(new CreateClient(id, url, scriptFile));
                            break;
                        }
                        case "Status": {
                            expectedMessage = $"{Environment.NewLine}Expected: Status";

                            if (words.Length != 1) {
                                throw new IncorrectCommandException(i, expectedMessage);
                            }

                            script.AddNode(new Status());
                            break;
                        }
                        case "Crash": {
                            expectedMessage = $"{Environment.NewLine}Expected: Crash server_id";

                            if (words.Length != 2) {
                                throw new IncorrectCommandException(i, expectedMessage);
                            }

                            string processName = words[1];
                            script.AddNode(new Crash(processName));
                            break;
                        }
                        case "Freeze": {
                            expectedMessage = $"{Environment.NewLine}Expected: Freeze server_id";

                            if (words.Length != 2) {
                                throw new IncorrectCommandException(i, expectedMessage);
                            }

                            string processName = words[1];
                            script.AddNode(new Freeze(processName));
                            break;
                        }
                        case "Unfreeze": {
                            expectedMessage = $"{Environment.NewLine}Expected: Unfreeze server_id";

                            if (words.Length != 2) {
                                throw new IncorrectCommandException(i, expectedMessage);
                            }

                            string processName = words[1];
                            script.AddNode(new Unfreeze(processName));
                            break;
                        }
                        case "Wait": {
                            expectedMessage = $"{Environment.NewLine}Expected: Wait x_ms";

                            if (words.Length != 2) {
                                throw new IncorrectCommandException(i, expectedMessage);
                            }

                            int time = int.Parse(words[1]);
                            script.AddNode(new Wait(time));
                            break;
                        }
                        case "Exit": {
                            expectedMessage = $"{Environment.NewLine}Expected: Exit";

                            if (words.Length != 1) {
                                throw new IncorrectCommandException(i, expectedMessage);
                            }

                            script.AddNode(new Exit());
                            break;
                        }
                        default:
                            throw new IncorrectCommandException(i, expectedMessage);
                    }
                } catch (Exception e) {
                    if (e is IncorrectCommandException) {
                        throw;
                    } else {
                        throw new IncorrectCommandException(i, expectedMessage);
                    }
                }
            }
            return script;
        }
    }
}