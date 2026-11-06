package f4.unit.fire
{
   public class FireTimer
   {
      
      private var _unfireTimer:uint;
      
      public var timer:uint;
      
      public var fireFrame:int;
      
      public var stopFrame:int;
      
      public var fireEx:int;
      
      public var firstFrame:int;
      
      public function FireTimer(param1:int, param2:int, param3:int, param4:int = 0)
      {
         super();
         this.timer = param4;
         this.fireFrame = param1;
         this.stopFrame = param2;
         this.fireEx = param3;
         this.firstFrame = param4;
         this._unfireTimer = 0;
      }
      
      public function get isFireFrame() : Boolean
      {
         ++this.timer;
         var _loc1_:int = this.timer % (this.fireFrame + this.stopFrame);
         if(_loc1_ < this.fireFrame && this.timer % this.fireEx == 0)
         {
            this._unfireTimer = 0;
            return true;
         }
         return false;
      }
      
      public function unFire() : void
      {
         if(this._unfireTimer < 108 && this.timer % (this.fireFrame + this.stopFrame) >= this.fireFrame)
         {
            this._unfireTimer = 107;
         }
         ++this._unfireTimer;
         if(this._unfireTimer == 108)
         {
            this.timer = this.fireFrame;
         }
         else if(this._unfireTimer > 108)
         {
            ++this.timer;
            if(this.timer % (this.fireFrame + this.stopFrame + this.firstFrame) == 0)
            {
               --this.timer;
            }
         }
      }
   }
}

