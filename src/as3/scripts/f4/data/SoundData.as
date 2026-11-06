package f4.data
{
   import flash.media.Sound;
   import flash.system.ApplicationDomain;
   
   public class SoundData
   {
      
      public var soundArray:Array;
      
      public var DOMAIN:ApplicationDomain;
      
      public var musicArray:Array;
      
      public var songNames:Array;
      
      public function SoundData()
      {
         super();
      }
      
      public function initArray() : void
      {
         this.soundArray = [];
         this.musicArray = [];
         this.songNames = [];
         this.soundArray.push(this.attachSound("S_Fire100"));
         this.soundArray.push(this.attachSound("S_FireGT"));
         this.soundArray.push(this.attachSound("S_FireL1"));
         this.soundArray.push(this.attachSound("S_FireL2"));
         this.soundArray.push(this.attachSound("S_Bomb"));
         this.soundArray.push(this.attachSound("S_Hit"));
         this.soundArray.push(this.attachSound("S_Pow"));
         this.soundArray.push(this.attachSound("S_Rocket"));
         this.soundArray.push(this.attachSound("S_Fuel"));
         this.soundArray.push(this.attachSound("S_Money"));
         this.soundArray.push(this.attachSound("S_Repair"));
         this.soundArray.push(this.attachSound("S_Weapon"));
         this.soundArray.push(this.attachSound("S_Zin"));
         this.soundArray.push(this.attachSound("S_Cant"));
         this.soundArray.push(this.attachSound("S_Check"));
         this.soundArray.push(this.attachSound("S_Word"));
         this.soundArray.push(this.attachSound("S_Zout"));
         this.soundArray.push(this.attachSound("S_Yes"));
         this.soundArray.push(this.attachSound("S_No"));
         this.soundArray.push(this.attachSound("S_Start"));
         this.soundArray.push(this.attachSound("S_Eng"));
         this.musicArray.push(this.attachSound("M_Main"));
         this.musicArray.push(this.attachSound("M_Menu"));
         this.musicArray.push(this.attachSound("M_Game1"));
         this.musicArray.push(this.attachSound("M_Game2"));
         this.musicArray.push(this.attachSound("M_Game3"));
         this.musicArray.push(this.attachSound("M_Game4"));
         this.musicArray.push(this.attachSound("M_Game5"));
         this.musicArray.push(this.attachSound("M_Game6"));
         this.musicArray.push(this.attachSound("M_Game7"));
         this.musicArray.push(this.attachSound("M_Creidts"));
         this.musicArray.push(this.attachSound("M_Win2"));
         this.songNames[0] = "Silence And Calm";
         this.songNames[1] = "Forging Skills";
         this.songNames[2] = "Rocking Adrenaline";
         this.songNames[3] = "Rock It One More Time";
         this.songNames[4] = "Rocking The Right Way";
         this.songNames[5] = "Gritty Punkster";
         this.songNames[6] = "Running From Shame";
         this.songNames[7] = "Raw Rivalry";
         this.songNames[8] = "Success As One";
         this.songNames[9] = "Her Scent";
         this.songNames[10] = "Rocking Country Man";
      }
      
      public function attachSound(param1:String) : Sound
      {
         var _loc2_:Class = null;
         _loc2_ = this.DOMAIN.getDefinition(param1) as Class;
         return new _loc2_();
      }
   }
}

