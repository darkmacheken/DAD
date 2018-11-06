using System;
using System.Collections.Generic;
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
            List<string> matches = new List<string>();
            string pattern = @"(\"".*?\"")+|((\d)+)|(\w)*?(\(.*?\))|(\w)+";

            Regex rgx = new Regex(pattern);
            foreach (Match match in rgx.Matches(tuple))
                matches.Add(match.Value);

            foreach (var match in matches) {

                //if string
                if (match.StartsWith("\"", StringComparison.Ordinal)) {
                    Field<string> field = new Field<string>(match.Substring(1, match.Length - 2)); //remove the quotes
                    this.Fields.Add(field);
                }

                //if object
                // objects are represented by a string array like ["nameofobject/class", [args]]
                else {
                    char[] charSeparators = { ',', '(', ')' };
                    string[] res = match.Split(charSeparators);
                    Field<object> field = new Field<object>(res);
                    this.Fields.Add(field);
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
    }

    // Each field has 2 properties: the type of the field and its value.
    public interface IField {
        Type Type { get; set; }
        object Value { get; set; }
    }

    public class Field<DataType> : IField {

        public Field(DataType value) {
            Value = value;
            Type = value.GetType();
        }

        public Type Type { get; set; }

        public object Value { get; set; }
    }
}
