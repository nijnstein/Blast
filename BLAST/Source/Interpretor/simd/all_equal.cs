//############################################################################################################################
//                                                                                                                           #
//  ██████╗ ██╗      █████╗ ███████╗████████╗                           Copyright © 2022 Rob Lemmens | NijnStein Software    #
//  ██╔══██╗██║     ██╔══██╗██╔════╝╚══██╔══╝                                    <rob.lemmens.s31 gmail com>                 #
//  ██████╔╝██║     ███████║███████╗   ██║                                           All Rights Reserved                     #
//  ██╔══██╗██║     ██╔══██║╚════██║   ██║                                                                                   #
//  ██████╔╝███████╗██║  ██║███████║   ██║     V1.0.4e                                                                       #
//  ╚═════╝ ╚══════╝╚═╝  ╚═╝╚══════╝   ╚═╝                                                                                   #
//                                                                                                                           #
//############################################################################################################################
//                                                                                                                     (__)  #
//       Unauthorized copying of this file, via any medium is strictly prohibited proprietary and confidential         (oo)  #
//                                                                                                                     (__)  #
//############################################################################################################################
#pragma warning disable CS1591
#pragma warning disable CS0162



using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Mathematics;

namespace NSS.Blast.SSMD
{
    unsafe public partial struct simd
    {


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool all_equal([NoAlias]in float* f1, in int datacount)
        {
            if (datacount > 1)
            {
                float a = f1[0];
                for(int i = 1; i < datacount; i++)
                {
                    if (a != f1[i]) return false; 
                }
            }
            return true; 
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool all_equal_x([NoAlias] in float4* f4, in int datacount)
        {
            if (datacount > 1)
            {
                float a = f4[0].x;
                for (int i = 1; i < datacount; i++)
                {
                    if (a != f4[i].x) return false;
                }
            }
            return true;
        }

    }
}
