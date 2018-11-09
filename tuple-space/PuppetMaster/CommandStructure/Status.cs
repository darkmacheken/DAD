using System;
using PuppetMaster.Visitor;

namespace PuppetMaster.CommandStructure {
    public class Status : Command {

        public Status() {}

        public override void Accept(IBasicVisitor v) {
            v.VisitStatus(this);
        }

    }
}
