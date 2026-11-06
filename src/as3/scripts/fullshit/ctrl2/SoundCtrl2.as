package fullshit.ctrl2
{
   import f4data.sdata.ConfigAndStory;
   import flash.media.Sound;
   import flash.media.SoundChannel;
   import flash.media.SoundTransform;
   import flash.utils.Dictionary;
   
   public class SoundCtrl2
   {
      
      public var musicDic:Dictionary;
      
      public var nowVol:Number = 1;
      
      public function SoundCtrl2()
      {
         super();
         this.musicDic = new Dictionary();
      }
      
      public function addSound(param1:SoundUnit2, param2:int, param3:Number = 0.01, param4:Number = 0.5, param5:Number = 0.01, param6:Boolean = false) : void
      {
         if(!this.musicDic[param1.soundName])
         {
            this.musicDic[param1.soundName] = param1;
            param1.init(param2,param3,param4,param5,param6);
         }
         else
         {
            (this.musicDic[param1.soundName] as SoundUnit2).toRun(-1,param4,param5,param6);
         }
      }
      
      public function s2Run(param1:String, param2:Number, param3:Number, param4:Number, param5:Boolean) : void
      {
         var _loc6_:SoundUnit2 = this.musicDic[param1];
         if(_loc6_)
         {
            _loc6_.toRun(param2,param3,param4,param5);
         }
      }
      
      public function runSound2() : void
      {
         var _loc1_:SoundUnit2 = null;
         var _loc2_:* = undefined;
         for(_loc2_ in this.musicDic)
         {
            _loc1_ = this.musicDic[_loc2_];
            _loc1_.vRun();
            if(_loc1_.singleEnd || _loc1_.vEnd && !_loc1_.ch)
            {
               trace(_loc2_);
               delete this.musicDic[_loc2_];
            }
         }
      }
      
      public function allTo(param1:Number, param2:Number, param3:Boolean, param4:String = null) : void
      {
         var _loc5_:SoundUnit2 = null;
         for each(_loc5_ in this.musicDic)
         {
            if((!param4 || param4 != _loc5_.soundName) && (param3 || !_loc5_.vEnd))
            {
               _loc5_.toRun(-1,param1,param2,param3);
            }
         }
      }
      
      public function allEnd(param1:Boolean, param2:String = null) : void
      {
         var _loc3_:SoundUnit2 = null;
         for each(_loc3_ in this.musicDic)
         {
            if((!param2 || param2 != _loc3_.soundName) && (param1 || !_loc3_.vEnd))
            {
               _loc3_.toRun(-1,0.01,-0.01,param1);
            }
         }
      }
      
      public function allResume() : void
      {
         var _loc1_:SoundUnit2 = null;
         for each(_loc1_ in this.musicDic)
         {
            if(!_loc1_.vEnd)
            {
               _loc1_.toRun(-1,this.nowVol,0.05,false);
            }
         }
      }
      
      public function playSoundOnce(param1:Sound, param2:Number = 1) : void
      {
         var ch:SoundChannel = null;
         var st:SoundTransform = null;
         var sound:Sound = param1;
         var v:Number = param2;
         if(ConfigAndStory.CONFIG[3] == 1)
         {
            try
            {
               ch = sound.play();
               st = ch.soundTransform;
               st.volume = v;
               ch.soundTransform = st;
            }
            catch(e:Error)
            {
               trace(e);
            }
         }
      }
   }
}

