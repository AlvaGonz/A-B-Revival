package f4ui.f4partmc
{
   import f4.data.InitData;
   import f4data.F4DataHole;
   import f4data.F4DataLinks;
   import f4data.F4DataOrg;
   import flash.display.DisplayObject;
   import flash.display.MovieClip;
   import flash.events.Event;
   import flash.events.MouseEvent;
   import ui.pages.UIMakePlayer;
   import ui.unit.FormPlayerPart;
   
   public class F4PartMC extends BaseMovePart
   {
      
      public var f4DataLink:F4DataLinks;
      
      public var allf4:Array;
      
      public var lock:Boolean;
      
      public var form:FormPlayerPart;
      
      public var delPartAct:Function;
      
      public var undo:UIMakePlayer;
      
      public var alpharun:Boolean;
      
      public function F4PartMC(param1:MovieClip, param2:Boolean)
      {
         super(param1);
         this.f4DataLink = new F4DataLinks();
         this.f4DataLink.f4mc = this;
         this.lock = param2;
         this.alpharun = false;
      }
      
      override public function mouseOver(param1:MouseEvent) : void
      {
         mc.dispatchEvent(new OrgEvent("Org",this.f4DataLink.f4dataorg,true));
      }
      
      override public function stageClick(param1:MouseEvent) : void
      {
         if(!this.lock && follow)
         {
            follow = false;
            this.allFollow(this,follow);
            stage.removeEventListener(MouseEvent.MOUSE_UP,this.stageClick);
            this.linkAct();
            this.changePartAlpha(this,1);
            this.alpharun = false;
            this.hidePoints();
         }
      }
      
      override public function mouseClick(param1:MouseEvent) : void
      {
         if(!this.lock && !follow)
         {
            follow = true;
            this.allFollow(this,follow);
            stage.addEventListener(MouseEvent.MOUSE_UP,this.stageClick);
            this.looseLink();
            this.alpharun = true;
         }
      }
      
      override public function loop(param1:Event) : void
      {
         if(!this.lock && follow)
         {
            this.mc2Mouse();
         }
         if(this.alpharun)
         {
            this.showPartMoving(this);
         }
      }
      
      public function allFollow(param1:F4PartMC, param2:Boolean) : void
      {
         var _loc3_:F4DataHole = null;
         if(param2)
         {
            param1.follow = true;
            param1.startFollow();
         }
         else
         {
            param1.follow = false;
         }
         for each(_loc3_ in param1.f4DataLink.f4dataorg.hole)
         {
            if(_loc3_.partf4)
            {
               this.allFollow(_loc3_.partf4.f4mc,param2);
            }
         }
      }
      
      public function allFinalMove(param1:F4PartMC) : void
      {
         var _loc2_:F4DataHole = null;
         if(param1.f4DataLink.atHole)
         {
            param1.f4DataLink.point.x = param1.f4DataLink.atHole.xy.x + param1.f4DataLink.parentPart.point.x - param1.f4DataLink.f4dataorg.linkOff.x;
            param1.f4DataLink.point.y = param1.f4DataLink.atHole.xy.y + param1.f4DataLink.parentPart.point.y - param1.f4DataLink.f4dataorg.linkOff.y;
            param1.mc.x = param1.f4DataLink.point.x;
            param1.mc.y = param1.f4DataLink.point.y;
         }
         for each(_loc2_ in param1.f4DataLink.f4dataorg.hole)
         {
            if(_loc2_.partf4)
            {
               this.allFinalMove(_loc2_.partf4.f4mc);
            }
         }
      }
      
      public function removeAllNotBack(param1:F4PartMC, param2:Boolean = true) : void
      {
         var _loc4_:F4DataHole = null;
         param1.mc.addEventListener(MouseEvent.MOUSE_OVER,this.mouseOver);
         param1.stage.removeEventListener(MouseEvent.MOUSE_UP,this.stageClick);
         param1.mc.removeEventListener(MouseEvent.MOUSE_DOWN,this.mouseClick);
         param1.mc.removeEventListener(Event.ENTER_FRAME,this.loop);
         param1.parent.removeChild(param1.mc);
         if(param2)
         {
            for each(_loc4_ in param1.f4DataLink.f4dataorg.hole)
            {
               if(_loc4_.partf4)
               {
                  this.removeAllNotBack(_loc4_.partf4.f4mc);
               }
            }
         }
         var _loc3_:int = this.allf4.indexOf(param1);
         this.allf4[_loc3_] = this.allf4[this.allf4.length - 1];
         --this.allf4.length;
      }
      
      public function removeAll(param1:F4PartMC, param2:Boolean = true) : void
      {
         var _loc4_:F4DataHole = null;
         param1.stage.removeEventListener(MouseEvent.MOUSE_UP,this.stageClick);
         param1.mc.removeEventListener(MouseEvent.MOUSE_DOWN,this.mouseClick);
         param1.mc.removeEventListener(Event.ENTER_FRAME,this.loop);
         param1.parent.removeChild(param1.mc);
         param1.f4DataLink.f4dataorg.useAs = 0;
         if(this.form)
         {
            this.form.pushOrg(param1.f4DataLink.f4dataorg);
         }
         if(this.undo)
         {
            this.undo.addToUndo(param1.f4DataLink.f4dataorg);
         }
         if(this.delPartAct != null)
         {
            this.delPartAct();
         }
         if(param2)
         {
            for each(_loc4_ in param1.f4DataLink.f4dataorg.hole)
            {
               if(_loc4_.partf4)
               {
                  this.removeAll(_loc4_.partf4.f4mc);
                  _loc4_.partf4 = null;
               }
            }
         }
         var _loc3_:int = this.allf4.indexOf(param1);
         this.allf4[_loc3_] = this.allf4[this.allf4.length - 1];
         --this.allf4.length;
      }
      
      public function removeAllNoFly(param1:F4PartMC, param2:Boolean = true) : void
      {
         var _loc4_:F4DataHole = null;
         param1.stage.removeEventListener(MouseEvent.MOUSE_UP,this.stageClick);
         param1.mc.removeEventListener(MouseEvent.MOUSE_DOWN,this.mouseClick);
         param1.mc.removeEventListener(Event.ENTER_FRAME,this.loop);
         param1.parent.removeChild(param1.mc);
         param1.f4DataLink.f4dataorg.useAs = 0;
         if(this.form)
         {
            this.form.pushOrg(param1.f4DataLink.f4dataorg);
         }
         if(this.undo)
         {
            this.undo.addToUndo(param1.f4DataLink.f4dataorg);
         }
         if(this.delPartAct != null)
         {
            this.delPartAct();
         }
         var _loc3_:int = this.allf4.indexOf(param1);
         this.allf4[_loc3_] = this.allf4[this.allf4.length - 1];
         --this.allf4.length;
         if(param2)
         {
            for each(_loc4_ in param1.f4DataLink.f4dataorg.hole)
            {
               if(_loc4_.partf4)
               {
                  this.removeAllNoFly(_loc4_.partf4.f4mc);
               }
            }
         }
      }
      
      public function linkAct() : void
      {
         var _loc1_:F4PartMC = null;
         var _loc2_:F4DataHole = null;
         var _loc3_:Number = NaN;
         var _loc5_:F4DataHole = null;
         var _loc6_:F4PartMC = null;
         var _loc4_:Number = int.MAX_VALUE;
         for each(_loc1_ in this.allf4)
         {
            if(_loc1_ != this && _loc1_.f4DataLink.parentPart != this.f4DataLink)
            {
               for each(_loc2_ in _loc1_.f4DataLink.f4dataorg.hole)
               {
                  if(!_loc2_.partf4 && _loc2_.acceptable(this.f4DataLink) == 0)
                  {
                     _loc3_ = (_loc2_.xy.x + _loc1_.f4DataLink.point.x - (this.f4DataLink.point.x + this.f4DataLink.f4dataorg.linkOff.x)) * (_loc2_.xy.x + _loc1_.f4DataLink.point.x - (this.f4DataLink.point.x + this.f4DataLink.f4dataorg.linkOff.x)) + (_loc2_.xy.y + _loc1_.f4DataLink.point.y - (this.f4DataLink.point.y + this.f4DataLink.f4dataorg.linkOff.y)) * (_loc2_.xy.y + _loc1_.f4DataLink.point.y - (this.f4DataLink.point.y + this.f4DataLink.f4dataorg.linkOff.y));
                     if(_loc3_ < _loc4_)
                     {
                        _loc4_ = _loc3_;
                        _loc5_ = _loc2_;
                        _loc6_ = _loc1_;
                     }
                  }
               }
            }
         }
         if(_loc4_ > 50 / 3 * 50 / 3)
         {
            InitData.SOUNDC2.playSoundOnce(InitData.SOUNDDATA.soundArray[18]);
            this.removeAll(this);
            if(this.form)
            {
               this.form.updatePages();
               this.form.filterX(this.form.filx);
            }
            trace("removeAll",_loc4_);
         }
         else if(_loc5_)
         {
            _loc5_.partf4 = this.f4DataLink;
            this.f4DataLink.atHole = _loc5_;
            this.f4DataLink.parentPart = _loc6_.f4DataLink;
            this.allFinalMove(this);
            InitData.SOUNDC2.playSoundOnce(InitData.SOUNDDATA.soundArray[14]);
         }
      }
      
      public function looseLink() : void
      {
         if(this.f4DataLink.atHole)
         {
            this.f4DataLink.atHole.partf4 = null;
            this.f4DataLink.atHole = null;
            trace("loose");
         }
         if(this.f4DataLink.parentPart)
         {
            this.f4DataLink.parentPart = null;
         }
      }
      
      public function readFromObj(param1:Object) : void
      {
         this.f4DataLink.readFromObj(param1,this.allf4);
         this.reLinkParent();
      }
      
      public function writeObj() : Object
      {
         var _loc1_:Object = new Object();
         _loc1_["link"] = this.f4DataLink.writeObj();
         return _loc1_;
      }
      
      public function reLinkParent() : void
      {
         this.f4LinkMC(this);
         this.allFinalMove(this);
      }
      
      public function f4LinkMC(param1:F4PartMC) : void
      {
         var _loc2_:F4DataHole = null;
         var _loc3_:int = int(param1.f4DataLink.f4dataorg.hole.length);
         var _loc4_:int = 0;
         while(_loc4_ < _loc3_)
         {
            _loc2_ = param1.f4DataLink.f4dataorg.hole[_loc4_];
            if(_loc2_.partf4)
            {
               _loc2_.partf4.parentPart = param1.f4DataLink;
               this.f4LinkMC(_loc2_.partf4.f4mc);
            }
            _loc4_++;
         }
      }
      
      public function mc2Mouse() : void
      {
         this.f4DataLink.point.x = parent.mouseX + startxy.x;
         this.f4DataLink.point.y = parent.mouseY + startxy.y;
         mc.x = this.f4DataLink.point.x;
         mc.y = this.f4DataLink.point.y;
      }
      
      public function makeFly() : Object
      {
         return this.f4DataLink.writeObj();
      }
      
      public function showPartMoving(param1:F4PartMC) : void
      {
         var _loc2_:int = 0;
         var _loc3_:F4PartMC = null;
         var _loc4_:F4DataHole = null;
         var _loc5_:Number = NaN;
         var _loc8_:F4PartMC = null;
         this.hidePoints();
         var _loc6_:Number = int.MAX_VALUE;
         var _loc7_:int = -1;
         for each(_loc3_ in this.allf4)
         {
            if(_loc3_ != param1 && _loc3_.f4DataLink.parentPart != param1.f4DataLink)
            {
               _loc2_ = 0;
               while(_loc2_ < _loc3_.f4DataLink.f4dataorg.hole.length)
               {
                  _loc4_ = _loc3_.f4DataLink.f4dataorg.hole[_loc2_];
                  if(!_loc4_.partf4 && _loc4_.acceptable(param1.f4DataLink) == 0)
                  {
                     _loc5_ = (_loc4_.xy.x + _loc3_.f4DataLink.point.x - (param1.f4DataLink.point.x + param1.f4DataLink.f4dataorg.linkOff.x)) * (_loc4_.xy.x + _loc3_.f4DataLink.point.x - (param1.f4DataLink.point.x + param1.f4DataLink.f4dataorg.linkOff.x)) + (_loc4_.xy.y + _loc3_.f4DataLink.point.y - (param1.f4DataLink.point.y + param1.f4DataLink.f4dataorg.linkOff.y)) * (_loc4_.xy.y + _loc3_.f4DataLink.point.y - (param1.f4DataLink.point.y + param1.f4DataLink.f4dataorg.linkOff.y));
                     if(_loc5_ < _loc6_)
                     {
                        _loc6_ = _loc5_;
                        _loc7_ = _loc2_;
                        _loc8_ = _loc3_;
                     }
                  }
                  _loc2_++;
               }
            }
         }
         if(_loc6_ > 50 / 3 * 50 / 3)
         {
            this.changePartAlpha(param1,0.3);
         }
         else if(_loc7_ >= 0)
         {
            this.showHole(_loc7_,_loc8_.mc);
            this.changePartAlpha(param1,0.75);
         }
      }
      
      public function changePartAlpha(param1:F4PartMC, param2:Number) : void
      {
         var _loc3_:F4DataHole = null;
         param1.mc.alpha = param2;
         for each(_loc3_ in param1.f4DataLink.f4dataorg.hole)
         {
            if(Boolean(_loc3_.partf4) && Boolean(_loc3_.partf4.f4mc))
            {
               this.changePartAlpha(_loc3_.partf4.f4mc,param2);
            }
         }
      }
      
      public function hidePoints() : void
      {
         var _loc1_:MovieClip = null;
         var _loc2_:int = 0;
         var _loc3_:int = 0;
         var _loc4_:DisplayObject = null;
         var _loc5_:String = null;
         var _loc6_:F4PartMC = null;
         for each(_loc6_ in this.allf4)
         {
            _loc1_ = _loc6_.mc;
            _loc3_ = _loc1_.numChildren;
            _loc2_ = 0;
            while(_loc2_ < _loc3_)
            {
               _loc4_ = _loc1_.getChildAt(_loc2_);
               _loc5_ = _loc4_.name;
               if(_loc5_.charAt(0) == "L")
               {
                  _loc4_.visible = false;
               }
               else if(_loc5_.charAt(0) == "H")
               {
                  _loc4_.visible = false;
               }
               else if(_loc5_.charAt(0) == "F")
               {
                  _loc4_.visible = false;
               }
               else if(_loc5_.charAt(0) == "P")
               {
                  _loc4_.visible = false;
               }
               _loc2_++;
            }
         }
      }
      
      public function showPoint(param1:F4DataOrg) : void
      {
         var _loc2_:int = 0;
         var _loc3_:F4DataHole = null;
         var _loc4_:F4PartMC = null;
         for each(_loc4_ in this.allf4)
         {
            _loc2_ = 0;
            while(_loc2_ < _loc4_.f4DataLink.f4dataorg.hole.length)
            {
               _loc3_ = _loc4_.f4DataLink.f4dataorg.hole[_loc2_];
               if(!_loc3_.partf4 && _loc3_.acceptableOrg(param1) == 0)
               {
                  this.showHole(_loc2_,_loc4_.mc);
               }
               _loc2_++;
            }
         }
      }
      
      public function showHole(param1:int, param2:MovieClip) : void
      {
         var _loc3_:int = 0;
         var _loc4_:int = 0;
         var _loc5_:DisplayObject = null;
         var _loc6_:String = null;
         var _loc7_:int = -1;
         _loc4_ = param2.numChildren;
         _loc3_ = 0;
         while(_loc3_ < _loc4_)
         {
            _loc5_ = param2.getChildAt(_loc3_);
            _loc6_ = _loc5_.name;
            if(_loc6_.charAt(0) == "H")
            {
               if(++_loc7_ == param1)
               {
                  _loc5_.visible = true;
               }
            }
            _loc3_++;
         }
      }
   }
}

