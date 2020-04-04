using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Alan {
    class JSON {

        public static JSONElement Parse(string json) {
            json = "\"root\":" + json;

            bool Quoted = false;
            bool SquareBrackets = false;

            Dictionary<int, JSONElement> Open = new Dictionary<int, JSONElement>();

            a: for (int i = 0; i < json.Length; i++) {
                if ((json[i] == '"' && i - 1 < 0) || (json[i] == '"' && i - 1 >= 0 && json[i - 1] != '\\')) {
                    Quoted = !Quoted;
                    continue;
                }

                if (json[i] == '[' && !Quoted) {
                    SquareBrackets = true;
                    continue;
                }
                if (json[i] == ']' && !Quoted) {
                    SquareBrackets = false;
                    continue;
                }

                if (json[i] == '{' && !Quoted) {

                    int OpenCount = 0;
                    int FoundCount = 0;

                    bool Quoted2 = false;

                    for (int j = i; j < json.Length; j++) {

                        if (json[j] == '"' && json[j - 1] != '\\') {
                            Quoted2 = !Quoted2;
                            continue;
                        }
                        
                        if (json[j] == '{' && !Quoted2) {
                            OpenCount++;
                            continue;
                        }
                        if (json[j] == '}' && !Quoted2) {
                            FoundCount++;
                            if (OpenCount == FoundCount) {

                                bool Quoted3 = false;
                                int Brackets = 0;
                                for (int k = j; k >= 0; k--) {
                                    if (json[k] == '"' && (k == 0 || (k - 1 >= 0 && json[k - 1] != '\\'))) {
                                        Quoted3 = !Quoted3;
                                        continue;
                                    }
                                    if (json[k] == '{' && !Quoted3) {
                                        Brackets++;
                                        continue;
                                    }
                                    if (json[k] == '}' && !Quoted3) {
                                        Brackets--;
                                        continue;
                                    }
                                }

                                string Name = "";
                                if (json[i - 1] == ':') {
                                    bool IsOpen = false;
                                    for (int k = i - 2; k >= 0; k--) {
                                        if (json[k] == '"' && (k == 0 || (k - 1 >= 0 && json[k - 1] != '\\'))) {
                                            IsOpen = !IsOpen;
                                            if (IsOpen == false) {
                                                Name = json.Substring(k + 1, i - 3 - k);
                                                break;
                                            }
                                        }
                                    }
                                }

                                string JsonObject = json.Substring(i, j - i + 1);
                                JSONElement JsonElement = new JSONElement();
                                JsonElement.v = JsonObject;

                                int BracketsCount = 0;
                                foreach (char c in JsonObject) {
                                    if (c == '{' || c == '}') BracketsCount++;
                                }

                                if (BracketsCount == 2) {
                                    JsonElement.CreateValues(JsonObject);
                                }

                                Open[Brackets] = JsonElement;
                                if (Brackets > 0) {
                                    Open[Brackets - 1].c.Add(Name, JsonElement);
                                }
                                break;
                            }

                            continue;
                        }

                    }
                    continue;
                }
            }

            return Open[0];
        }

        public static string Stringify(JSONElement root) {
            string s = "";

            if (root.c.Count > 0) {
                s += "{";
                foreach (string k in root.c.Keys) {
                    s += "\"" + k + "\":" + Stringify(root.c[k]) + ", ";
                }
                s = s.Substring(0, s.Length - 2);
                s += "}";
            }
            else s += root.v;

            return s;
        }

    }

    class JSONElement {
        public Dictionary<string, JSONElement> c = new Dictionary<string, JSONElement>();
        public object v;
        public void CreateValues(string s) {

            string key = "", value = "";
            bool Quoted = false;
            int QuoteStart = 0;
            for (int i = 1; i < s.Length - 1; i++) {
                if (s[i] == '"' && (i == 0 || (i - 1 >= 0 && s[i - 1] != '\\'))) {
                    Quoted = !Quoted;
                    if (Quoted) QuoteStart = i;
                    else {
                        key = s.Substring(QuoteStart + 1, i - QuoteStart - 1);
                        int QuoteStart2 = 0;

                        value = "";

                        if (s[i + 1] == ':') {
                            bool Quoted2 = false;
                            if (s[i + 2] == '"') {
                                for (int j = i + 2; j < s.Length - 1; j++) {
                                    if (s[j] == '"' && s[j - 1] != '\\') {
                                        Quoted2 = !Quoted2;
                                        if (!Quoted2) {
                                            value = s.Substring(QuoteStart2 + 1, j - QuoteStart2 - 1);
                                            i = j;
                                            break;
                                        }
                                        else QuoteStart2 = j;
                                    }
                                }
                            } else {
                                for (int j = i + 2; j < s.Length - 1; j++) {
                                    if (s[j] == ',' || s[j] == '{' || s[j] == '{' || s[j] == ':') {
                                        break;
                                    }
                                    else value += s[j];
                                }
                            }
                        }

                        Console.WriteLine($"Found key: {key} with value: {value}");
                        JSONElement e = new JSONElement();
                        e.v = value;
                        c.Add(key, e);
                    }
                    continue;
                }
            }
        }
        public override string ToString() {
            return "" + v;
        }
        public int ToInt() {
            return (int)v;
        }
    }

}
