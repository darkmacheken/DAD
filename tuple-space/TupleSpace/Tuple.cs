using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace TupleSpace {

    public class Tuple {
        // A tuple consists in a list of fields.
        public List<IField> Fields { get; set; }
        public bool Locked { get; set; }

        public Tuple(string tuple) {
            this.Fields = new List<IField>();
            this.Locked = false;
            this.ParseInput(tuple);
        }

        public bool Match(Tuple searchTuple) {
           
            if (this.Fields.Count != searchTuple.Fields.Count) {
                return false;
            }

            /* Check all fields of the tuple*/
            for (int i = 0; i < this.Fields.Count; i++) {
                /* if field is a string */
                if (this.Fields[i].Type == typeof(string) && searchTuple.Fields[i].Type == typeof(string) && 
                    StringUtils.MatchString(this.Fields[i].Value.ToString(), searchTuple.Fields[i].Value.ToString())) {

                    continue;
                }

                /* if field is an object */
                if (this.Fields[i].Type != typeof(string)) {

                    /* if search is an object too, check if it is equals */
                    if (searchTuple.Fields[i].Type != typeof(string) &&
                        ((dynamic) this.Fields[i].Value).Equals((dynamic) searchTuple.Fields[i].Value)) {

                        continue;
                    }
                    /* if search is a string, check if its the name of the type of the object or null */
                    else if (searchTuple.Fields[i].Type == typeof(string)) {
                        if (searchTuple.Fields[i].Value.Equals("null")) {
                            continue;
                        } else {
                            string fullClassName = string.Concat("TupleSpace.", searchTuple.Fields[i].Value);
                            Type searchType = Type.GetType(fullClassName);

                            if (searchType != null && this.Fields[i].Type.Equals(searchType)) {
                                continue;
                            }
                        }
                    }
                }
                return false;
            }
            return true;
        }

        public override string ToString() {
            string fields = string.Empty;
            foreach (IField field in this.Fields) {
                fields = string.Concat(fields, field.Value, " ");
            }
            return $"Tuple: <{fields}>";
        }

        private void ParseInput(string tuple) {

            List<string> fields = new List<string>(); //stores the parameters of the tuple.
            string pattern = @"(\"".*?\"")+|((\d)+)|(\w)*?(\(.*?\))|(\w)+";
            Regex rgx = new Regex(pattern);

            foreach (Match match in rgx.Matches(tuple)) {
                fields.Add(match.Value);
            }

            foreach (string field in fields) {
                /* If the field is a string */
                if (field.StartsWith("\"", StringComparison.Ordinal)) {
                    /* Add to Tuple fields*/
                    Field<string> newField = new Field<string>(field.Substring(1, field.Length - 2)); //remove the quotes
                    this.Fields.Add(newField);
                } else { /* The field is an object */
                    char[] charSeparators = { ',', '(', ')' };
                    string[] res = field.Split(charSeparators);

                    /* If it's a name - className or null */
                    if (res.Length == 1) {
                        /* Add to Tuple fields*/
                        Field<string> newField = new Field<string>(field);
                        this.Fields.Add(newField);
                    } else { /* It's a constructor */
                        string className = res[0];
                        List<string> args = res.ToList().GetRange(1, res.Length - 2); //removes the className and the last match (empty)
                        List<object> parsedArgs = this.ParseArgs(args);

                        /* Instantiate object from the given constructor */
                        Type t = Type.GetType(string.Concat("TupleSpace.", className));
                        object obj = Activator.CreateInstance(t, parsedArgs.ToArray());

                        /* Add to Tuple fields*/
                        Field<object> newField = new Field<object>(obj);
                        this.Fields.Add(newField);
                    }
                }
            }
        }

        private List<object> ParseArgs(List<string> args) {
            List<object> parsedArgs = new List<object>();

            foreach (string arg in args) {
                if (arg.Length <= 0) { //empty match
                    continue;
                }
                if (arg.Contains("\"")) { //if arg is a string
                    string parsedArg = Regex.Replace(arg, @"(\s|\"")*", string.Empty);
                    parsedArgs.Add(parsedArg);
                } else { //if arg is an int
                    int parsedArg = int.Parse(arg);
                    parsedArgs.Add(parsedArg);
                }
            }
            return parsedArgs;
        }
    }


    // Each field has 2 properties: the type of the field and its value.
    public interface IField {
        Type Type { get; set; }

        object Value { get; set; }

        string ToString();
    }

    public class Field<TDataType> : IField {

        public Field(TDataType value) {
            this.Value = value;
            this.Type = value.GetType();
        }

        public Type Type { get; set; }

        public object Value { get; set; }

        public override string ToString() {
            return $"{this.Type}({this.Value})";
        }
    }
}
