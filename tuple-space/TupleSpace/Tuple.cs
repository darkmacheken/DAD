using System;
using System.Collections.Generic;

namespace TupleSpace {

    public class Tuple {
        // A tuple consists in a list of fields.
        public List<IField> fields = new List<IField>();

        public static Tuple ParseTuple(string tuple) {
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
