package f4.data
{
   import f4.evts.EvtCtrl;
   import f4.level.LevelBase;
   import f4.unit.ai.AIPart;
   import f4.unit.ai.AIUnity;
   import f4.unit.move.MoveAs;
   import f4.unit.outer.AIOuter;
   import f4.unit.particle.DeadPart;
   import f4.unit.particle.Item;
   import f4.unit.particle.Particle;
   import f4.unit.parts.SimpleFly;
   import f4.unit.parts.Unity;
   import f4.unit.swave.SWave;
   import f4data.sdata.ConfigAndStory;
   import flash.display.BitmapData;
   import flash.display.MovieClip;
   import flash.geom.Point;
   import flash.geom.Rectangle;
   import flash.text.TextField;
   import flash.utils.ByteArray;
   import fullshit.ds.Array1;
   import fullshit.tool.Rect2;
   import fullshit.tool.StaticShit;
   
   public class Targets
   {
      
      public var eba1:Array1;
      
      public var efa1:Array1;
      
      public var pba1:Array1;
      
      public var pfa1:Array1;
      
      public var pa1:Array1;
      
      public var swa1:Array1;
      
      public var swa2:Array1;
      
      public var ia1:Array1;
      
      public var ta1:Array1;
      
      public var outa1:Array1;
      
      public var deadParta1:Array1;
      
      private var _allPools:Array;
      
      private var _rect2:Rect2;
      
      private var _ckRect:Rectangle;
      
      public var evts:EvtCtrl;
      
      public var playerRect:Rectangle;
      
      public var hitRectPoints:HitRectPoints;
      
      public var levelTimer:uint;
      
      private var _maptimer:uint;
      
      public var startPoint:Point;
      
      public function Targets()
      {
         super();
         this.startPoint = new Point();
         this.eba1 = new Array1();
         this.efa1 = new Array1();
         this.pba1 = new Array1();
         this.pfa1 = new Array1();
         this.pa1 = new Array1();
         this.swa1 = new Array1();
         this.swa2 = new Array1();
         this.ia1 = new Array1();
         this.ta1 = new Array1();
         this.outa1 = new Array1();
         this.deadParta1 = new Array1();
         this._allPools = [this.outa1, this.eba1, this.efa1, this.pba1, this.pfa1, this.pa1, this.swa1, this.swa2, this.ia1, this.ta1, this.deadParta1];
         this.hitRectPoints = new HitRectPoints();
         this._rect2 = StaticShit.RECT2;
         this._ckRect = new Rectangle();
         this.playerRect = new Rectangle(0,0,3200,3200);
         this.evts = new EvtCtrl();
      }
      
      public function initEvts() : void
      {
         var _loc2_:Object = null;
         var _loc4_:Number = NaN;
         var _loc5_:AIUnity = null;
         var _loc6_:Number = NaN;
         var _loc7_:Number = NaN;
         var _loc8_:int = 0;
         ConfigAndStory.ALLPART.repairPart.length = 0;
         ConfigAndStory.ALLPART.itemPart.length = 0;
         ConfigAndStory.ALLPART.levelMoney = 0;
         var _loc1_:LevelBase = new LevelBase();
         var _loc3_:ByteArray = new ByteArray();
         _loc3_.writeObject(Fly4UI.LVDATA["" + (ConfigAndStory.STORY + 1)]);
         _loc3_.position = 0;
         _loc2_ = _loc3_.readObject();
         _loc1_.initFromObj(_loc2_);
         if(ConfigAndStory.STORY != 49)
         {
            _loc6_ = InitData.CORE.playerFly.unity.move.point.x;
            _loc7_ = InitData.CORE.playerFly.unity.move.point.y;
            var _capUnit:AIUnity = this.evts.getAIByPerson("Captain");
            _loc8_ = 0;
            var _compCount:int = ConfigAndStory.COMPANIONS ? ConfigAndStory.COMPANIONS.length : 0;
            while(_loc8_ < _compCount)
            {
               var _compRecord:Object = ConfigAndStory.COMPANIONS[_loc8_];
               if(_compRecord != null && _compRecord.craftIndex >= 0)
               {
                  var _cRole:int = _compRecord.role;
                  var _cLeader:int = _compRecord.leader;
                  var _targetLeaderUnit:AIUnity = null;
                  var _spawnLeaderName:String = "Michael";
                  var _sX:Number = _loc6_ + (_loc8_ + 1) * 80;
                  var _sY:Number = _loc7_;

                  if(_cLeader == ConfigAndStory.LEADER_CAPTAIN && _capUnit != null && _capUnit.unity != null && _capUnit.unity.move != null)
                  {
                     _targetLeaderUnit = _capUnit;
                     _spawnLeaderName = "Captain";
                     _sX = _capUnit.unity.move.point.x + (_loc8_ + 1) * 80;
                     _sY = _capUnit.unity.move.point.y;
                  }
                  else
                  {
                     _targetLeaderUnit = InitData.CORE.playerFly;
                     _spawnLeaderName = "Michael";
                  }

                  this.evts.addAI(_sX,_sY,_spawnLeaderName,["AI_P"],0,_compRecord.craftIndex,true,false,2,_cRole,_targetLeaderUnit);
               }
               _loc8_++;
            }
         }
         for each(_loc5_ in this.efa1.array)
         {
            _loc4_ = Math.atan2(_loc7_ - _loc5_.unity.move.point.y,_loc6_ - _loc5_.unity.move.point.x);
            _loc5_.unity.move.radFace = _loc5_.unity.move.radFire = _loc4_;
         }
         if(ConfigAndStory.STORY == 49)
         {
            InitData.CORE.playerFly.unity.move.radFace = InitData.CORE.playerFly.unity.move.radFire = Math.PI / 2;
         }
         InitData.CORE.cam.rect.x = InitData.CORE.playerFly.unity.move.point.x;
         InitData.CORE.cam.rect.y = InitData.CORE.playerFly.unity.move.point.y;
      }
      
      public function uiinit() : void
      {
         var _loc1_:AIUnity = null;
         for each(_loc1_ in this.pfa1.array)
         {
            if(_loc1_.pilotName.indexOf("Captain") != -1 || _loc1_.pilotName.indexOf("Tom") != -1)
            {
               if(this.evts.gameui.pf1)
               {
                  this.evts.gameui.pf2 = _loc1_;
                  break;
               }
               this.evts.gameui.pf1 = _loc1_;
            }
         }
         if(!this.evts.gameui.pf1)
         {
            this.evts.gameui.f1.visible = false;
         }
         else
         {
            (this.evts.gameui.f1.getChildByName("face") as MovieClip).gotoAndStop(this.evts.gameui.pf1.pilotName[0]);
            this.evts.gameui.f1.visible = true;
         }
         if(!this.evts.gameui.pf2)
         {
            this.evts.gameui.f2.visible = false;
         }
         else
         {
            (this.evts.gameui.f2.getChildByName("face") as MovieClip).gotoAndStop(this.evts.gameui.pf2.pilotName[0]);
            this.evts.gameui.f2.visible = true;
         }
         (this.evts.gameui.lifeBar.getChildByName("face") as MovieClip).gotoAndStop(InitData.CORE.playerFly.pilotName[0]);
         (this.evts.gameui.lifeBar.getChildByName("txtName") as TextField).text = InitData.CORE.playerFly.pilotName[0];
      }
      
      public function reset() : void
      {
         this._maptimer = this.levelTimer = 0;
         if(this._allPools)
         {
            for each(var pool:Array1 in this._allPools)
            {
               if(pool)
               {
                  pool.removeSelf1();
               }
            }
         }
         if(InitData.SOUNDC32)
         {
            InitData.SOUNDC32.clean();
         }
         this.evts.clean();
      }
      
      public function runTargets() : void
      {
         this.ckOuterRound();
         this.outa1.run1();
         this.efa1.run1();
         this.pfa1.run1();
         this.eba1.run1();
         this.pba1.run1();
         this.swa1.run1();
         this.swa2.run1();
         this.ia1.run1();
         this.ta1.run1();
         this.runSW();
         this.evts.runClock();
         this.evts.runSpoint();
         this.evts.runWinEvt();
         this.evts.runWin1Evt();
         this.deadParta1.run1();
      }
      
      public function ckOuterRound() : void
      {
         var _loc1_:AIOuter = null;
         for each(_loc1_ in this.outa1.array)
         {
            if(!_loc1_.roundStop)
            {
               return;
            }
         }
         for each(_loc1_ in this.outa1.array)
         {
            _loc1_.roundStop = false;
         }
      }
      
      public function bltHit(param1:AIUnity) : void
      {
         var _loc2_:AIUnity = null;
         var _loc3_:AIUnity = null;
         var _loc4_:SimpleFly = null;
         this.updatePlayerRect(param1);
         this.baCkF(this.eba1.array,param1);
         for each(_loc2_ in this.efa1.array)
         {
            if(_loc2_.playerRange > 0)
            {
               this.baCkFE(this.pba1.array,_loc2_);
            }
         }
         for each(_loc3_ in this.pfa1.array)
         {
            if(_loc3_.playerRange > 0)
            {
               this.baCkF(this.eba1.array,_loc3_);
            }
         }
         for each(_loc4_ in this.pba1.array)
         {
            if(_loc4_.ckblt.hitPart)
            {
               _loc4_.ckblt.hitPart.beHitBlt(_loc4_);
               if(_loc4_.data.hp.x <= 0)
               {
                  _loc4_.move.point.x = _loc4_.move.lastPoint.x + _loc4_.ckblt.mvx * _loc4_.ckblt.short;
                  _loc4_.move.point.y = _loc4_.move.lastPoint.y + _loc4_.ckblt.mvy * _loc4_.ckblt.short;
                  _loc4_.deadSpd = new Point(_loc4_.move.spdp.x,_loc4_.move.spdp.y);
                  _loc4_.move.spdp.x = _loc4_.move.spdp.y = 0;
               }
            }
            if(_loc4_.ckblt.hp2 && _loc4_.data.hp.x < 1)
            {
               _loc4_.data.hp.x = 1;
            }
         }
         for each(_loc4_ in this.eba1.array)
         {
            if(_loc4_.ckblt.hitPart)
            {
               _loc4_.ckblt.hitPart.beHitBlt(_loc4_);
               if(_loc4_.data.hp.x <= 0)
               {
                  _loc4_.move.point.x = _loc4_.move.lastPoint.x + _loc4_.ckblt.mvx * _loc4_.ckblt.short;
                  _loc4_.move.point.y = _loc4_.move.lastPoint.y + _loc4_.ckblt.mvy * _loc4_.ckblt.short;
                  _loc4_.deadSpd = new Point(_loc4_.move.spdp.x,_loc4_.move.spdp.y);
                  _loc4_.move.spdp.x = _loc4_.move.spdp.y = 0;
               }
            }
            if(_loc4_.ckblt.hp2 && _loc4_.data.hp.x < 1)
            {
               _loc4_.data.hp.x = 1;
            }
         }
         if(param1 && !param1.isDeadA1 && param1.unity)
         {
            this.iaCkItem(this.ia1.array,param1);
         }
         var _loc10_:AIUnity = null;
         for each(_loc10_ in this.pfa1.array)
         {
            if(_loc10_ && !_loc10_.isDeadA1 && _loc10_.unity)
            {
               this.iaCkItem(this.ia1.array,_loc10_);
            }
         }
      }
      
      public function runSW() : void
      {
         var _loc1_:AIUnity = null;
         var _loc2_:Array = this.swa1.array;
         for each(_loc1_ in this.efa1.array)
         {
            if(_loc1_.aimodeNum < 4 && _loc1_.playerRange > 0)
            {
               this.ckSWU(_loc1_.unity,_loc2_);
            }
         }
         _loc2_ = this.swa2.array;
         for each(_loc1_ in this.pfa1.array)
         {
            if(_loc1_.aimodeNum < 4 && _loc1_.playerRange > 0)
            {
               this.ckSWU(_loc1_.unity,_loc2_);
            }
         }
      }
      
      public function ckSW(param1:Rectangle, param2:MoveAs, param3:Number) : void
      {
         var _loc4_:Number = NaN;
         var _loc5_:Number = NaN;
         var _loc6_:Number = NaN;
         var _loc8_:SWave = null;
         var _loc7_:Boolean = false;
         for each(_loc8_ in this.swa1.array)
         {
            if(this._rect2.rectIntersects(param2.hitRect,_loc8_.rect))
            {
               _loc4_ = (param2.point.x - _loc8_.point.x) * (param2.point.x - _loc8_.point.x);
               _loc5_ = (param2.point.y - _loc8_.point.y) * (param2.point.y - _loc8_.point.y);
               _loc6_ = (param1.width / 2 + _loc8_.r) * (param1.width / 2 + _loc8_.r);
               if(_loc4_ + _loc5_ < _loc6_)
               {
                  param2.spdp.x += _loc8_.pow * (param2.point.x - _loc8_.point.x) / param3;
                  param2.spdp.y += _loc8_.pow * (param2.point.y - _loc8_.point.y) / param3;
                  _loc7_ = true;
               }
            }
         }
         _loc7_ && param2.d2r(param2.spdp,param2.spd);
      }
      
      public function ckSWU(param1:Unity, param2:Array) : void
      {
         var _loc3_:Number = NaN;
         var _loc4_:Number = NaN;
         var _loc5_:AIPart = null;
         var _loc6_:SWave = null;
         var _loc7_:Number = NaN;
         var _loc8_:Number = NaN;
         var _loc9_:Boolean = false;
         for each(_loc6_ in param2)
         {
            _loc9_ = false;
            if(this._rect2.rectIntersects(param1.move.hitRect,_loc6_.rect))
            {
               for each(_loc5_ in param1.parts)
               {
                  if(_loc5_.onLoad.length == 0 && this._rect2.rectIntersects(_loc5_.part.partMove.hitRect,_loc6_.rect))
                  {
                     _loc7_ = Math.atan2(_loc6_.point.y - _loc5_.part.partMove.point.y,_loc6_.point.x - _loc5_.part.partMove.point.x) - _loc5_.part.partMove.radFace;
                     _loc8_ = (_loc6_.point.y - _loc5_.part.partMove.point.y) * (_loc6_.point.y - _loc5_.part.partMove.point.y) + (_loc6_.point.x - _loc5_.part.partMove.point.x) * (_loc6_.point.x - _loc5_.part.partMove.point.x);
                     _loc3_ = _loc8_ * Math.cos(_loc7_);
                     _loc4_ = _loc8_ * Math.sin(_loc7_);
                     if(_loc3_ < 0)
                     {
                        _loc3_ = -_loc3_;
                     }
                     if(_loc4_ < 0)
                     {
                        _loc4_ = -_loc4_;
                     }
                     if(_loc3_ < (_loc5_.part.hitRectOrg.x / 2 + _loc6_.r) * (_loc5_.part.hitRectOrg.x / 2 + _loc6_.r) && _loc4_ < (_loc5_.part.hitRectOrg.y / 2 + _loc6_.r) * (_loc5_.part.hitRectOrg.y / 2 + _loc6_.r))
                     {
                        param1.move.spdp.x += 0.1 * _loc6_.pow * (_loc5_.part.partMove.point.x - _loc6_.point.x) / param1.udata.mass;
                        param1.move.spdp.y += 0.1 * _loc6_.pow * (_loc5_.part.partMove.point.y - _loc6_.point.y) / param1.udata.mass;
                        _loc5_.beHitSW(_loc6_);
                        _loc9_ = true;
                     }
                  }
               }
            }
            _loc9_ && param1.move.d2r(param1.move.spdp,param1.move.spd);
         }
      }
      
      public function renderBlt() : void
      {
         var _loc1_:SimpleFly = null;
         for each(_loc1_ in this.pba1.array)
         {
            _loc1_.renderAct();
         }
         for each(_loc1_ in this.eba1.array)
         {
            _loc1_.renderAct();
         }
      }
      
      public function renderPart() : void
      {
         var _loc1_:Particle = null;
         for each(_loc1_ in this.pa1.array)
         {
            _loc1_.render();
         }
      }
      
      public function runRender(param1:AIUnity) : void
      {
         var _loc2_:AIUnity = null;
         var _loc3_:Item = null;
         var _loc4_:DeadPart = null;
         for each(_loc3_ in this.ia1.array)
         {
            _loc3_.render();
         }
         for each(_loc2_ in this.efa1.array)
         {
            _loc2_.render();
         }
         for each(_loc2_ in this.pfa1.array)
         {
            _loc2_.render();
         }
         param1.render();
         for each(_loc4_ in this.deadParta1.array)
         {
            _loc4_.render();
         }
      }
      
      public function showLife(param1:AIUnity) : void
      {
         var _loc2_:AIUnity = null;
         param1.showLife();
         for each(_loc2_ in this.efa1.array)
         {
            _loc2_.showLife();
         }
         for each(_loc2_ in this.pfa1.array)
         {
            _loc2_.showLife();
         }
      }
      
      public function baCkFE(param1:Array, param2:AIUnity) : void
      {
         var _loc3_:SimpleFly = null;
         var _loc4_:AIPart = null;
         var _loc5_:Boolean = false;
         var _loc6_:int = 0;
         for each(_loc3_ in param1)
         {
            if(this._rect2.rectIntersects(_loc3_.ckRect,param2.unity.move.hitRect))
            {
               for each(_loc4_ in param2.unity.parts)
               {
                  if(this._rect2.rectIntersects(_loc3_.ckRect,_loc4_.part.partMove.hitRect))
                  {
                     this._ckRect.x = _loc3_.move.lastRect.x;
                     this._ckRect.y = _loc3_.move.lastRect.y;
                     this._ckRect.width = _loc3_.move.lastRect.width;
                     this._ckRect.height = _loc3_.move.lastRect.height;
                     _loc5_ = false;
                     _loc6_ = 0;
                     while(_loc6_ < _loc3_.ckblt.loop)
                     {
                        this._ckRect.x += _loc3_.ckblt.mvx;
                        this._ckRect.y += _loc3_.ckblt.mvy;
                        if(this._rect2.rectIntersects(this._ckRect,_loc4_.part.partMove.hitRect))
                        {
                           if(this.hitRectPoints.rectHitPoints(_loc4_.part.partMove.point,_loc4_.part.hitpoints4,this._ckRect))
                           {
                              _loc5_ = true;
                              break;
                           }
                        }
                        _loc6_++;
                     }
                     if(!_loc5_)
                     {
                        if(this._rect2.rectIntersects(_loc3_.move.hitRect,_loc4_.part.partMove.hitRect))
                        {
                           if(this.hitRectPoints.rectHitPoints(_loc4_.part.partMove.point,_loc4_.part.hitpoints4,_loc3_.move.hitRect))
                           {
                              _loc5_ = true;
                           }
                        }
                     }
                     if(_loc5_)
                     {
                        if(_loc3_.ckblt.hp2 || _loc3_.data.hp.x > 1)
                        {
                           _loc3_.ckblt.hp2 = true;
                           if(_loc4_.part.main)
                           {
                              _loc4_.beHitBlt(_loc3_);
                              break;
                           }
                           (param2.unity.parts[0] as AIPart).beHitBlt(_loc3_);
                           _loc4_.beHitBlt(_loc3_);
                        }
                        else if(_loc3_.ckblt.short > _loc6_)
                        {
                           _loc3_.ckblt.short = _loc6_;
                           _loc3_.ckblt.hitPart = param2.unity.parts[0] as AIPart;
                           if(_loc4_.part.main)
                           {
                              break;
                           }
                           _loc4_.beHitBlt(_loc3_);
                        }
                     }
                  }
               }
            }
         }
      }
      
      public function baCkF(param1:Array, param2:AIUnity) : void
      {
         var _loc3_:SimpleFly = null;
         var _loc4_:AIPart = null;
         var _loc5_:Boolean = false;
         var _loc6_:int = 0;
         for each(_loc3_ in param1)
         {
            if(this._rect2.rectIntersects(_loc3_.ckRect,param2.unity.move.hitRect))
            {
               for each(_loc4_ in param2.unity.parts)
               {
                  if(_loc4_.onLoad.length == 0 && this._rect2.rectIntersects(_loc3_.ckRect,_loc4_.part.partMove.hitRect))
                  {
                     this._ckRect.x = _loc3_.move.lastRect.x;
                     this._ckRect.y = _loc3_.move.lastRect.y;
                     this._ckRect.width = _loc3_.move.lastRect.width;
                     this._ckRect.height = _loc3_.move.lastRect.height;
                     _loc5_ = false;
                     _loc6_ = 0;
                     while(_loc6_ < _loc3_.ckblt.loop)
                     {
                        this._ckRect.x += _loc3_.ckblt.mvx;
                        this._ckRect.y += _loc3_.ckblt.mvy;
                        if(this._rect2.rectIntersects(this._ckRect,_loc4_.part.partMove.hitRect))
                        {
                           if(this.hitRectPoints.rectHitPoints(_loc4_.part.partMove.point,_loc4_.part.hitpoints4,this._ckRect))
                           {
                              _loc5_ = true;
                              break;
                           }
                        }
                        _loc6_++;
                     }
                     if(!_loc5_)
                     {
                        if(this._rect2.rectIntersects(_loc3_.move.hitRect,_loc4_.part.partMove.hitRect))
                        {
                           if(this.hitRectPoints.rectHitPoints(_loc4_.part.partMove.point,_loc4_.part.hitpoints4,_loc3_.move.hitRect))
                           {
                              _loc5_ = true;
                           }
                        }
                     }
                     if(_loc5_)
                     {
                        if(_loc3_.ckblt.hp2 || _loc3_.data.hp.x > 1)
                        {
                           _loc4_.beHitBlt(_loc3_);
                           _loc3_.ckblt.hp2 = true;
                        }
                        else if(_loc3_.ckblt.short > _loc6_)
                        {
                           _loc3_.ckblt.short = _loc6_;
                           _loc3_.ckblt.hitPart = _loc4_;
                        }
                     }
                  }
               }
            }
         }
      }
      
      public function iaCkItem(param1:Array, param2:AIUnity) : void
      {
         var _loc3_:Item = null;
         var _loc4_:AIPart = null;
         for each(_loc3_ in param1)
         {
            if(!_loc3_.isDeadA1 && this._rect2.rectIntersects(_loc3_.hit,param2.unity.move.hitRect))
            {
               for each(_loc4_ in param2.unity.parts)
               {
                  if(this._rect2.rectIntersects(_loc3_.hit,_loc4_.part.partMove.hitRect))
                  {
                     if(this.hitRectPoints.rectHitPoints(_loc4_.part.partMove.point,_loc4_.part.hitpoints4,_loc3_.hit))
                     {
                        _loc3_.beHit(param2);
                        break;
                     }
                  }
               }
            }
         }
      }
      
      public function updatePlayerRect(param1:AIUnity) : void
      {
         this.playerRect.x = param1.unity.move.point.x - this.playerRect.width / 2;
         this.playerRect.y = param1.unity.move.point.y - this.playerRect.height / 2;
      }
      
      public function drawMiniMap(param1:AIUnity) : void
      {
         var _loc2_:AIUnity = null;
         var _loc9_:SimpleFly = null;
         var _loc3_:BitmapData = InitData.RENDERDATA.MINIMAP;
         var _loc4_:Number = _loc3_.width / this.playerRect.width;
         var _loc5_:Number = _loc3_.height / this.playerRect.height;
         var _loc6_:Number = this.playerRect.x;
         var _loc7_:Number = this.playerRect.y;
         var _loc8_:Rectangle = new Rectangle();
         _loc3_.fillRect(new Rectangle((InitData.CORE.cam.rect.x - _loc6_) * _loc4_,(InitData.CORE.cam.rect.y - _loc7_) * _loc5_,InitData.CORE.cam.rect.width * _loc4_,InitData.CORE.cam.rect.height * _loc5_),1090519039);
         ++this._maptimer;
         if(this._maptimer % 10 < 5)
         {
            _loc8_.x = (this.startPoint.x - 2.5 / _loc4_ - _loc6_) * _loc4_;
            _loc8_.y = (this.startPoint.y - 2.5 / _loc4_ - _loc7_) * _loc5_;
            _loc8_.width = 5;
            _loc8_.height = 5;
            if(_loc8_.x < 0)
            {
               _loc8_.x = 0;
            }
            else if(_loc8_.x + _loc8_.width > _loc3_.width)
            {
               _loc8_.x = _loc3_.width - _loc8_.width;
            }
            if(_loc8_.y < 0)
            {
               _loc8_.y = 0;
            }
            else if(_loc8_.y + _loc8_.height > _loc3_.height)
            {
               _loc8_.y = _loc3_.height - _loc8_.height;
            }
            _loc3_.fillRect(_loc8_,4278255360);
         }
         for each(_loc2_ in this.efa1.array)
         {
            _loc8_.x = (_loc2_.unity.move.hitRect.x - _loc6_) * _loc4_;
            _loc8_.y = (_loc2_.unity.move.hitRect.y - _loc7_) * _loc5_;
            _loc8_.width = _loc2_.unity.move.hitRect.width * _loc4_;
            _loc8_.height = _loc2_.unity.move.hitRect.height * _loc5_;
            _loc3_.fillRect(_loc8_,4294901760);
         }
         for each(_loc2_ in this.pfa1.array)
         {
            _loc8_.x = (_loc2_.unity.move.hitRect.x - _loc6_) * _loc4_;
            _loc8_.y = (_loc2_.unity.move.hitRect.y - _loc7_) * _loc5_;
            _loc8_.width = _loc2_.unity.move.hitRect.width * _loc4_;
            _loc8_.height = _loc2_.unity.move.hitRect.height * _loc5_;
            if(_loc8_.x < 0)
            {
               _loc8_.x = 0;
            }
            else if(_loc8_.x + _loc8_.width > _loc3_.width)
            {
               _loc8_.x = _loc3_.width - _loc8_.width;
            }
            if(_loc8_.y < 0)
            {
               _loc8_.y = 0;
            }
            else if(_loc8_.y + _loc8_.height > _loc3_.height)
            {
               _loc8_.y = _loc3_.height - _loc8_.height;
            }
            _loc3_.fillRect(_loc8_,4278255360);
         }
         if(Boolean(this.evts.spoint) && this._maptimer % 10 < 5)
         {
            _loc8_.x = (this.evts.spoint.x - _loc6_) * _loc4_;
            _loc8_.y = (this.evts.spoint.y - _loc7_) * _loc5_;
            _loc8_.width = 4;
            _loc8_.height = 4;
            if(_loc8_.x < 0)
            {
               _loc8_.x = 0;
            }
            else if(_loc8_.x + _loc8_.width > _loc3_.width)
            {
               _loc8_.x = _loc3_.width - _loc8_.width;
            }
            if(_loc8_.y < 0)
            {
               _loc8_.y = 0;
            }
            else if(_loc8_.y + _loc8_.height > _loc3_.height)
            {
               _loc8_.y = _loc3_.height - _loc8_.height;
            }
            _loc3_.fillRect(_loc8_,4291585536);
         }
         for each(_loc9_ in this.pba1.array)
         {
            _loc3_.setPixel32((_loc9_.move.point.x - _loc6_) * _loc4_,(_loc9_.move.point.y - _loc7_) * _loc5_,2147549183);
         }
         for each(_loc9_ in this.eba1.array)
         {
            _loc3_.setPixel32((_loc9_.move.point.x - _loc6_) * _loc4_,(_loc9_.move.point.y - _loc7_) * _loc5_,2164195583);
         }
         _loc8_.x = (param1.unity.move.hitRect.x - _loc6_) * _loc4_;
         _loc8_.y = (param1.unity.move.hitRect.y - _loc7_) * _loc5_;
         _loc8_.width = param1.unity.move.hitRect.width * _loc4_;
         _loc8_.height = param1.unity.move.hitRect.height * _loc5_;
         _loc3_.fillRect(_loc8_,2164260608);
      }
   }
}

