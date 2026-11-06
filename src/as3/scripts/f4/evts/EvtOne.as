package f4.evts
{
   public class EvtOne
   {
      
      public var evtName:String;
      
      public var require:Array;
      
      public var finish:Array;
      
      public var finishRemoveCF:Array;
      
      public var finishRemove:Array;
      
      public var finishRemoveClock:Array;
      
      public var finishAdd:Array;
      
      public var finishAddClock:Array;
      
      public var timeLimit:uint;
      
      public var looptime:int;
      
      public var always:Boolean;
      
      public var after:String;
      
      public var finishActs:Array;
      
      public var frameActs:Array;
      
      public var data:Object;
      
      public function EvtOne(param1:String)
      {
         super();
         this.evtName = param1;
         this.require = [];
         this.finish = [];
         this.finishAddClock = [];
         this.finishAdd = [];
         this.finishRemove = [];
         this.finishRemoveCF = [];
         this.finishRemoveClock = [];
         this.finishActs = [];
         this.frameActs = [];
         this.timeLimit = 0;
         this.always = false;
         this.data = new Object();
      }
   }
}

