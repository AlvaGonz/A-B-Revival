package fullshit.ctrl2
{
   import f4.data.InitData;
   import flash.events.Event;
   import flash.media.Sound;
   import flash.media.SoundChannel;
   import flash.media.SoundTransform;
   
   public class SoundUnit2
   {
      
      public var soundName:String;
      
      public var sound:Sound;
      
      public var ch:SoundChannel;
      
      public var vNow:Number;
      
      public var vAdd:Number;
      
      public var vTo:Number;
      
      public var vEnd:Boolean;
      
      public var sc2:SoundCtrl2;
      
      public var next:SoundUnit2;
      
      public var singleEnd:Boolean;
      
      public function SoundUnit2(param1:Sound, param2:String)
      {
         super();
         this.sc2 = InitData.SOUNDC2;
         this.sound = param1;
         this.soundName = param2;
         this.singleEnd = false;
      }
      
      public function init(param1:int, param2:Number = 0.01, param3:Number = 0.8, param4:Number = 0.01, param5:Boolean = false) : void
      {
         this.ch = this.sound.play(0,param1);
         this.ch.addEventListener(Event.SOUND_COMPLETE,this.soundComlpete);
         this.toRun(param2,param3,param4,param5);
      }
      
      private function soundComlpete(param1:Event) : void
      {
         if(!this.vEnd && Boolean(this.next))
         {
            this.next.singleEnd = false;
            this.sc2.addSound(this.next,1,0.01,this.vNow);
         }
         this.singleEnd = true;
         this.ch.removeEventListener(Event.SOUND_COMPLETE,this.soundComlpete);
         this.ch = null;
      }
      
      public function toRun(param1:Number, param2:Number, param3:Number, param4:Boolean) : void
      {
         var _loc5_:SoundTransform = null;
         if(param1 >= 0)
         {
            this.vNow = param1;
            _loc5_ = this.ch.soundTransform;
            _loc5_.volume = this.vNow;
            this.ch.soundTransform = _loc5_;
         }
         this.vTo = param2;
         this.vAdd = param3;
         this.vEnd = param4;
      }
      
      public function vRun() : void
      {
         var _loc1_:SoundTransform = null;
         if(!this.singleEnd)
         {
            if(this.vNow != this.vTo)
            {
               this.vNow += this.vAdd;
               if(this.vAdd > 0)
               {
                  if(this.vNow > this.vTo)
                  {
                     this.vNow = this.vTo;
                  }
               }
               else if(this.vAdd < 0)
               {
                  if(this.vNow < this.vTo)
                  {
                     this.vNow = this.vTo;
                  }
               }
               _loc1_ = this.ch.soundTransform;
               _loc1_.volume = this.vNow;
               this.ch.soundTransform = _loc1_;
            }
            else if(this.vEnd)
            {
               this.ch.removeEventListener(Event.SOUND_COMPLETE,this.soundComlpete);
               this.ch.stop();
               this.ch = null;
            }
         }
      }
   }
}

