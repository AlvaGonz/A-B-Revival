package f4.unit.ai
{
   import f4.data.InitData;
   import f4.data.Targets;
   import fullshit.ds.AbsOne;
   
   public class AIBase extends AbsOne
   {
      
      public var aif:AIFunctions = new AIFunctions();
      
      public var targets:Targets = InitData.TARGETS;
      
      public var act:int;
      
      public function AIBase()
      {
         super();
      }
   }
}

