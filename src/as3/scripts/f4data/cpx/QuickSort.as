package f4data.cpx
{
   public class QuickSort
   {
      
      public function QuickSort()
      {
         super();
      }
      
      public function sort(param1:Array, param2:int) : void
      {
         this._sort(param1,param2,0,param1.length - 1);
      }
      
      private function _sort(param1:Array, param2:int, param3:int, param4:int) : void
      {
         var _loc5_:* = undefined;
         var _loc6_:int = 0;
         var _loc7_:int = 0;
         var _loc8_:* = undefined;
         if(param3 < param4)
         {
            _loc5_ = param1[param3 + param4 >> 1][param2];
            _loc6_ = param3 - 1;
            _loc7_ = param4 + 1;
            while(true)
            {
               while(param1[++_loc6_][param2] < _loc5_)
               {
               }
               while(param1[--_loc7_][param2] > _loc5_)
               {
               }
               if(_loc6_ >= _loc7_)
               {
                  break;
               }
               _loc8_ = param1[_loc6_];
               param1[_loc6_] = param1[_loc7_];
               param1[_loc7_] = _loc8_;
            }
            this._sort(param1,param2,param3,_loc6_ - 1);
            this._sort(param1,param2,_loc7_ + 1,param4);
         }
      }
   }
}

