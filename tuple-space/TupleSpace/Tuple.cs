using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TupleSpace {

    public class Tuple {
        // A tuple consists in a list of fields.
        public List<IField> fields = new List<IField>();

        public Tuple ParseTuple(string tuple) {
        
            List<string> matches = new List<string>();
            string pattern = @"(\"".*?\"")+|((\d)+)|(\w)*?(\(.*?\))|(\w)+";

            Regex rgx = new Regex(pattern);
            foreach (Match match in rgx.Matches(tuple))
                matches.Add(match.Value);

            foreach (var match in matches) {
                //if string
                if (match.StartsWith("\"", StringComparison.Ordinal)) {
                    Field<string> field = new Field<string>(match.Substring(1, match.Length - 2)); //remove the quotes
                    this.fields.Add(field);
                }
                //if int
                else if (int.TryParse(match, out int number)) {
                    Field<int> field = new Field<int>(number);
                    this.fields.Add(field);
                }
                //if object
                else {
                    // TO DO
                }
            }

            return null;
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
