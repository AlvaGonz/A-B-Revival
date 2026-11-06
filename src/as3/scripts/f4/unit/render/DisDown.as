package f4.unit.render
{
   import flash.display.DisplayObject;
   import flash.geom.ColorTransform;
   import fullshit.ds.AbsOne;
   
   public class DisDown extends AbsOne
   {
      
      public var display:DisplayObject;
      
      public var ct:ColorTransform;
      
      public var cNum:Number;
      
      public function DisDown(param1:DisplayObject)
      {
         super();
         this.display = param1;
         this.cNum = 3;
         this.ct = new ColorTransform(this.cNum,this.cNum,this.cNum,1);
      }
      
      override public function runA1() : void
      {
         if(!this.display.stage)
         {
            isDeadA1 = true;
            return;
         }
         this.cNum *= 0.9;
         if(this.cNum < 1)
         {
            this.cNum = 1;
            isDeadA1 = true;
         }
         this.ct.redMultiplier = this.ct.greenMultiplier = this.ct.blueMultiplier = this.cNum;
         this.display.transform.colorTransform = this.ct;
      }
      
      override public function startRemoveA1() : void
      {
      }
   }
}

