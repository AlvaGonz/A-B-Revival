package f4.data
{
   import f4.core.OpOneCore;
   import f4.data.sound32.SoundCtrl32;
   import f4.gfx.GData;
   import f4.gfx.RenderBlur;
   import f4.gfx.RenderData;
   import f4.make.Maker;
   import f4.unit.ai.AIData;
   import f4data.sdata.ConfigAndStory;
   import flash.display.Sprite;
   import flash.display.Stage;
   import flash.net.URLRequest;
   import flash.net.navigateToURL;
   import fullshit.ctrl2.SoundCtrl2;
   import ui.UICtrl;
   
   public class InitData
   {
      
      public static var RENDERDATA:RenderData;
      
      public static var MAKER:Maker;
      
      public static var STAGE:Stage;
      
      public static var ROOT:Sprite;
      
      public static var CORE:OpOneCore;
      
      public static var TARGETS:Targets;
      
      public static var GDATA:GData;
      
      public static var BLUR:RenderBlur;
      
      public static var UICTRL:UICtrl;
      
      public static var SOUNDC2:SoundCtrl2;
      
      public static var SOUNDDATA:SoundData;
      
      public static var SOUNDC32:SoundCtrl32;
      
      public static var AIDATA:AIData;
      
      public static var autof:Number = 0.95;
      
      public static var lifeEnemy:Number = 0.8;
      
      public static var lifePlayer:Number = 1.35;
      
      public static var lifeAI:Number = 0.95;
      
      public static var powrateEnemy:Number = 0.8;
      
      public static var powratePlayer:Number = 0.92;
      
      public static var powrateAI:Number = 0.825;
      
      public static var powspdEnemy:Number = 0.62;
      
      public static var powspdPlayer:Number = 0.66;
      
      public static var powspdAI:Number = 0.63;
      
      public static var bltEnemy:Number = 0.63;
      
      public static var bltPlayer:Number = 1.1;
      
      public static var bltAI:Number = 0.75;
      
      public static const REPAIR:Number = 0.75;
      
      public static const SELL:Number = 0.7;
      
      public function InitData()
      {
         super();
      }
      
      public static function initAll(param1:Stage, param2:Sprite) : void
      {
         STAGE = param1;
         ROOT = param2;
         TARGETS = new Targets();
         RENDERDATA = new RenderData();
         MAKER = new Maker();
         GDATA = new GData();
         if(ConfigAndStory.ALLPART)
         {
            GDATA.initStage3DAtlas(ConfigAndStory.ALLPART);
         }
         CORE = new OpOneCore();
         BLUR = new RenderBlur();
         AIDATA = new AIData();
         SOUNDC2 = new SoundCtrl2();
         SOUNDC32 = new SoundCtrl32();
      }
      
      public static function vargames() : void
      {
         navigateToURL(new URLRequest("http://www.vargames.com"),"_blank");
      }
   }
}

