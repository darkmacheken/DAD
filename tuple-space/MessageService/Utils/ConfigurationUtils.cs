using System;
using System.Collections.Generic;
using System.Linq;

namespace StateMachineReplication.Utils {
    public static class ConfigurationUtils {
        public static bool CompareConfigurations(SortedDictionary<string, Uri> conf1,
            SortedDictionary<string, Uri> conf2) {
            if (conf1 == null || conf2 == null) {
                return false;
            }
            return conf1.Count == conf2.Count &&
                   conf1.Keys.All(key => conf2.ContainsKey(key) && Uri.Equals(conf1[key], conf2[key]));
        }
    }
}