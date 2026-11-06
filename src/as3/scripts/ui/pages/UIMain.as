package ui.pages
{
   import f4.data.InitData;
   import f4data.sdata.ConfigAndStory;
   import flash.display.DisplayObjectContainer;
   import flash.display.MovieClip;
   import flash.text.TextField;
   import fullshit.ctrl2.SoundUnit2;
   import fullshit.tool.Attach;
   import ui.UICtrl;
   import ui.base.BtnBase;
   import ui.base.UIBase;
   import ui.make.MakeUI;
   
   public class UIMain extends UIBase
   {
      
      public function UIMain(param1:DisplayObjectContainer, param2:MovieClip, param3:String, param4:UICtrl, param5:UIBase = null)
      {
         if(ConfigAndStory.TCONFIG)
         {
            ConfigAndStory.CONFIG = ConfigAndStory.TCONFIG;
            ConfigAndStory.TCONFIG = null;
            ConfigAndStory.makeChange();
         }
         (param2.getChildByName("word1") as TextField).text = Fly4UI.BUILD;
         if(!VeryOutShitABC.VGPLAY)
         {
            (param2.getChildByName("$word2") as TextField).text = "PLAY A&B-SPACE AT VARGAMES.COM TO GET POWER PACK! AND NO ADS!!";
         }
         else
         {
            (param2.getChildByName("$word2") as TextField).visible = false;
         }
         super(param1,param2,param3,param4,param5);
         InitData.SOUNDC2.allEnd(true);
         InitData.SOUNDC2.addSound(new SoundUnit2(InitData.SOUNDDATA.musicArray[0],"M_Main"),int.MAX_VALUE,0.01,InitData.SOUNDC2.nowVol);
      }
      
      override public function act(param1:BtnBase) : void
      {
         var _loc2_:String = param1.name;
         if(_loc2_ == "play")
         {
            InitData.SOUNDC2.playSoundOnce(InitData.SOUNDDATA.soundArray[19],1);
            removeSelf();
            MakeUI.makeChoose(this,_sp,_ui);
         }
         else if(_loc2_ == "option")
         {
            new UIOptions(_sp,Attach.attach("UI_Options") as MovieClip,"UI_Options",_ui,this);
         }
         else if(_loc2_ == "data")
         {
            new UIData(_sp,Attach.attach("UI_Data") as MovieClip,"UI_Data",_ui,this);
         }
         else if(_loc2_ == "credits")
         {
            if(ConfigAndStory.STORY < 99)
            {
               ConfigAndStory.S100LOCK = true;
               ConfigAndStory.S100LOCKINT = ConfigAndStory.STORY;
            }
            InitData.SOUNDC2.playSoundOnce(InitData.SOUNDDATA.soundArray[19],1);
            ConfigAndStory.STORY = 49;
            removeSelf();
            MakeUI.makeChoose(this,_sp,_ui);
         }
         else if(_loc2_ == "player")
         {
            new UIPlayer(_sp,Attach.attach("UI_Mission") as MovieClip,"UI_Player",_ui,this);
         }
         else if(_loc2_ == "word2")
         {
            InitData.vargames();
         }
         else if(_loc2_ == "link")
         {
            InitData.vargames();
         }
      }
   }
}

