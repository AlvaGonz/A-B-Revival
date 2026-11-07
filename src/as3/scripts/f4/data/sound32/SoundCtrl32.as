package f4.data.sound32
{
   import f4.data.InitData;
   import f4data.sdata.ConfigAndStory;
   import fullshit.ds.Array1;
   
   public class SoundCtrl32
   {
      
      public var soundRun:Array1;
      
      public var sfxa1:Array1;
      
      public var sound32_1:SoundUnit32;
      
      public var soundBuffer:Array;
      
      public function SoundCtrl32()
      {
         super();
         this.init();
      }
      
      public function init() : void
      {
         this.soundBuffer = [];
         this.soundRun = new Array1();
         this.sfxa1 = new Array1();
         this.sound32_1 = new SoundUnit32(InitData.SOUNDDATA.soundArray[0],0,true);
      }
      
      public function run() : void
      {
         this.soundRun.run1();
         this.sfxa1.run1();
      }
      
      public function runSR() : void
      {
         var _loc1_:SoundRun1 = null;
         var _loc2_:Array = this.soundRun.array;
         var _loc3_:Number = 0;
         var _loc4_:Number = 0;
         var _loc5_:Number = 0;
         for each(_loc1_ in _loc2_)
         {
            if(_loc1_.pan >= 0)
            {
               _loc4_ += _loc1_.vol;
               _loc3_ += _loc1_.vol * (1 - _loc1_.pan);
            }
            else
            {
               _loc3_ += _loc1_.vol;
               _loc4_ += _loc1_.vol * (1 + _loc1_.pan);
            }
            _loc5_ += _loc1_.vol;
         }
         this.sound32_1.pan = _loc4_ - _loc3_;
         if(_loc5_ > 0.2)
         {
            this.sound32_1.vol = 0.2;
         }
         else
         {
            this.sound32_1.vol = _loc5_;
         }
         this.sound32_1.runA1();
      }
      
      public function addSFX25(param1:SoundUBlt32) : void
      {
         var _loc2_:Array = null;
         var _loc3_:int = 0;
         var _loc4_:SoundUBlt32 = null;
         var _loc5_:Number = NaN;
         var _loc6_:Boolean = false;
         if(ConfigAndStory.CONFIG[3] == 1)
         {
            param1.ckVol();
            if(param1.vol > 0)
            {
               _loc2_ = this.sfxa1.array;
               _loc3_ = int(_loc2_.length);
               _loc6_ = true;
               for each(_loc4_ in _loc2_)
               {
                  if(_loc4_.timer < 5 && param1.stag == _loc4_.stag)
                  {
                     _loc5_ = param1.pan - _loc4_.pan;
                     if((_loc5_ < 0 ? -_loc5_ : _loc5_) < 0.23)
                     {
                        _loc4_.volMut += param1.volMut;
                        _loc6_ = false;
                        break;
                     }
                  }
               }
               if(_loc6_)
               {
                  if(_loc3_ < 28)
                  {
                     this.sfxa1.addOne1(param1);
                     param1.init();
                  }
               }
            }
         }
      }
   }
}

