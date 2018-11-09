using PuppetMaster.CommandStructure;

namespace PuppetMaster.Visitor {
    public interface IBasicVisitor {
        void VisitCrash(Crash crash);

        void VisitCreateClient(CreateClient createClient);

        void VisitCreateServer(CreateServer createServer);

        void VisitFreeze(Freeze freeze);

        void VisitScript(Script script);

        void VisitStatus(Status status);

        void VisitUnfreeze(Unfreeze unfreeze);

        void VisitWait(Wait wait);
    }
}