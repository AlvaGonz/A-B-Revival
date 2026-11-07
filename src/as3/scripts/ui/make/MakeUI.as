package ui.make
{
   import f4.data.InitData;
   import f4data.F4DataLinks;
   import f4data.sdata.ConfigAndStory;
   import flash.display.DisplayObjectContainer;
   import flash.display.MovieClip;
   import fullshit.tool.Attach;
   import ui.UICtrl;
   import ui.base.UIBase;
   import ui.pages.UIChooseFlyAI;
   import ui.pages.UIMapBuild;
   
   public class MakeUI
   {
      
      private static var _ui:UICtrl;
      
      private static var _sp:DisplayObjectContainer;
      
      public function MakeUI()
      {
         super();
      }
      
      public static function makeUI(param1:DisplayObjectContainer, param2:String, param3:UICtrl, param4:UIBase = null) : UIBase
      {
         return new UIBase(param1,Attach.attach(param2) as MovieClip,param2,param3,param4);
      }
      
      public static function makeChoose(param1:UIBase, param2:DisplayObjectContainer, param3:UICtrl, param4:UIBase = null) : UIBase
      {
         if(ConfigAndStory.nowChoose())
         {
            if(ConfigAndStory.AIGAME)
            {
               return new UIChooseFlyAI(ConfigAndStory.ALLPART,param2,Attach.attach("UI_ChooseFlyAI") as MovieClip,"UI_ChooseFlyAI",param3,param4);
            }
            return new UIChooseFlyAI(ConfigAndStory.ALLPART,param2,Attach.attach("UI_ChooseFlyAI") as MovieClip,"UI_ChooseFlyAI",param3,param4);
         }
         startGameDir(param1);
         return null;
      }
      
      public static function startGameDir(param1:UIBase) : void
      {
         var _loc2_:int = 0;
         _ui = param1.uictrl;
         _sp = param1.sp;
         ConfigAndStory.DIRECTOR.beforeLevelMov(playGame,param1);
         InitData.CORE.repair.flys.length = 0;
         InitData.CORE.levelData.afa.length = 0;
         if(ConfigAndStory.noNext())
         {
            InitData.CORE.repair.flys.push(ConfigAndStory.ALLPART.plFly[ConfigAndStory.PLAYER]);
         }
         var _capFly:F4DataLinks = ConfigAndStory.getCaptainFly();
         if(_capFly)
         {
            InitData.CORE.repair.flys.push(_capFly);
         }
         if(ConfigAndStory.STORY != 49 && ConfigAndStory.COMPANIONS)
         {
            _loc2_ = 0;
            while(_loc2_ < ConfigAndStory.COMPANIONS.length)
            {
               var _compEntry:Object = ConfigAndStory.COMPANIONS[_loc2_];
               if(_compEntry != null && _compEntry.craftIndex >= 0)
               {
                  InitData.CORE.levelData.afa.push(ConfigAndStory.ALLPART.plFly[_compEntry.craftIndex]);
                  InitData.CORE.repair.flys.push(ConfigAndStory.ALLPART.plFly[_compEntry.craftIndex]);
               }
               _loc2_++;
            }
         }
      }
      
      private static function playGame() : void
      {
         new UIMapBuild(_sp,Attach.attach("UI_MapBuilding") as MovieClip,"UI_MapBuilding",_ui);
         _ui = null;
         _sp = null;
      }
   }
}

