// Contour par coque inversée : on redessine le maillage en ne gardant que ses faces
// arrière, poussées vers l'extérieur le long de leurs normales. Le résultat entoure
// l'objet d'un liseré uni, ce qui donne au rendu un parti pris dessiné plutôt qu'un
// assemblage de volumes gris.
//
// L'épaisseur est appliquée en espace MONDE, jamais en espace objet. Les pièces de ce
// projet sont des primitives aux échelles très inégales — un poteau fait 0.18 sur deux
// axes et plus d'un mètre sur le troisième. Un décalage en espace objet y produirait un
// contour énorme sur les axes écrasés et invisible sur l'autre.
Shader "Bastion/Contour"
{
    Properties
    {
        _CouleurContour ("Couleur", Color) = (0.05, 0.05, 0.09, 1)
        _Epaisseur ("Épaisseur (mètres)", Range(0, 0.2)) = 0.025
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }

        Pass
        {
            Name "Contour"
            Cull Front          // seules les faces arrière : la coque n'apparaît qu'autour
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _CouleurContour;
                float  _Epaisseur;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normaleWS  = normalize(TransformObjectToWorldNormal(IN.normalOS));
                OUT.positionHCS = TransformWorldToHClip(positionWS + normaleWS * _Epaisseur);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                return half4(_CouleurContour.rgb, 1.0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
