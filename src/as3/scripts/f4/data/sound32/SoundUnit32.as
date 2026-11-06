package f4.data.sound32
{
   import flash.events.Event;
   import flash.media.Sound;
   import flash.media.SoundChannel;
   import flash.media.SoundTransform;
   import fullshit.ds.AbsOne;
   
   public class SoundUnit32 extends AbsOne
   {
      
      public var soundCh:SoundChannel;
      
      public var soundData:Sound;
      
      public var pan:Number;
      
      public var vol:Number;
      
      public var stag:int;
      
      public var loop:Boolean;
      
      public function SoundUnit32(param1:Sound, param2:int, param3:Boolean = false)
      {
         super();
         this.stag = param2;
         this.soundData = param1;
         this.pan = 0;
         this.vol = 0;
         this.loop = param3;
      }
      
      public function init() : void
      {
         if(this.loop)
         {
            this.soundCh = this.soundData.play(0,int.MAX_VALUE);
         }
         else
         {
            this.soundCh = this.soundData.play();
         }
         this.soundCh.addEventListener(Event.SOUND_COMPLETE,this.soundComlpete);
         this.runA1();
      }
      
      protected function soundComlpete(param1:Event) : void
      {
         isDeadA1 = true;
      }
      
      public function changeVol() : void
      {
         var _loc1_:SoundTransform = this.soundCh.soundTransform;
         _loc1_.volume = this.vol;
         this.soundCh.soundTransform = _loc1_;
      }
      
      public function changePan() : void
      {
         var _loc1_:SoundTransform = this.soundCh.soundTransform;
         _loc1_.pan = this.pan;
         this.soundCh.soundTransform = _loc1_;
      }
      
      public function changeVolPan() : void
      {
         var _loc1_:SoundTransform = this.soundCh.soundTransform;
         _loc1_.pan = this.pan;
         _loc1_.volume = this.vol;
         this.soundCh.soundTransform = _loc1_;
      }
      
      override public function startRemoveA1() : void
      {
         if(this.soundCh)
         {
            this.soundCh.removeEventListener(Event.SOUND_COMPLETE,this.soundComlpete);
            this.soundCh.stop();
         }
      }
      
      override public function runA1() : void
      {
         this.changeVolPan();
      }
   }
}

