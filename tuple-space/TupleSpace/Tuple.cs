using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;


namespace TupleSpace {

    public class Tuple {
        // A tuple consists in a list of fields.
        public List<IField> Fields { get; set; }

        public Tuple(string tuple) {
            Fields = new List<IField>();
            ParseInput(tuple);
        }

        void ParseInput(string tuple) {

            List<string> fields = new List<string>(); //stores the parameters of the tuple.
            string pattern = @"(\"".*?\"")+|((\d)+)|(\w)*?(\(.*?\))|(\w)+";
            Regex rgx = new Regex(pattern);

            foreach (Match match in rgx.Matches(tuple))
                fields.Add(match.Value);

            foreach (var field in fields) {
                /* If the field is a string */
                if (field.StartsWith("\"", StringComparison.Ordinal)) {
                    /* Add to Tuple fields*/
                    Field<string> new_field = new Field<string>(field.Substring(1, field.Length - 2)); //remove the quotes
                    this.Fields.Add(new_field);
                }

                /* If the field is an object */
                else {
                    char[] charSeparators = { ',', '(', ')' };
                    string[] res = field.Split(charSeparators);

                    /* If it's a name - className or null */
                    if (res.Length == 1) {
                        /* Add to Tuple fields*/
                        Field<string> new_field = new Field<string>(field);
                        this.Fields.Add(new_field);
                    }
                    /* If it's a constructor */
                    else {
                        string className = res[0];
                        List<string> args = res.ToList().GetRange(1, res.Length - 2); //removes the className and the last match (empty)
                        List<object> parsed_args = ParseArgs(args);

                        /* Instanciate object from the given constructor */
                        Type t = Type.GetType(String.Concat("TupleSpace.", className));
                        object obj = Activator.CreateInstance(t, parsed_args.ToArray());

                        /* Add to Tuple fields*/
                        Field<object> new_field = new Field<object>(obj);
                        this.Fields.Add(new_field);
                    }
                }
            }
        }

        public bool Match(Tuple search_tuple) {
            if (Fields.Count != search_tuple.Fields.Count) {
                return false;
            }

            //for each field
            for (int i = 0; i < Fields.Count; i++) {
                //if field is a string
                //TO DO WILDCARDS
                if (Fields[i].Type == typeof(string) && search_tuple.Fields[i].Type == typeof(string)
                    && Fields[i].Value.Equals(search_tuple.Fields[i].Value)) {
                    continue;
                }

                //if field is an object
                if (Fields[i].Type.IsArray && search_tuple.Fields[i].Type.IsArray) {
                    string[] args = (string[])Fields[i].Value;
                    string[] search_args = (string[])search_tuple.Fields[i].Value;

                    //if the search tuple is a null object
                    if (search_args[0].Equals("null")) {
                        continue;
                    }

                    //if the search tuple is only a class
                    if (search_args.Length == 1 && search_args[0].Equals(args[0])) {
                        continue;
                    }

                    //if the search tuple is a class with args
                    if (search_args.Length.Equals(args.Length) && Utils.CompareArrays(search_args, args)) {
                        continue;
                    }
                }
                return false;
            }
            return true;
        }

        public override string ToString() {
            string fields = "";
            foreach (IField field in Fields) {
                String.Concat(fields, field.Value, " ");
            }
            return "Tuple: " + fields;
        }

        List<object> ParseArgs(List<string> args) {
            List<object> parsed_args = new List<object>();

            foreach (string arg in args) {
                if (arg.Length <= 0) { //empty match
                    continue;
                }
                if (arg.Contains("\"")) { //if arg is a string
                    string parsed_arg = Regex.Replace(arg, @"(\s|\"")*", "");
                    parsed_args.Add(parsed_arg);
                }
                else { //if arg is an int
                    int parsed_arg = Int32.Parse(arg);
                    parsed_args.Add(parsed_arg);
                }
            }
            return parsed_args;
        }
    }

    // Each field has 2 properties: the type of the field and its value.
    public interface IField {
        Type Type { get; set; }
        object Value { get; set; }
        string ToString();
    }

    public class Field<DataType> : IField {

        public Field(DataType value) {
            Value = value;
            Type = value.GetType();
        }

        public Type Type { get; set; }

        public object Value { get; set; }

        public override string ToString()
        {
            return Type + "(" + Value + ")";
        }
    }
}
