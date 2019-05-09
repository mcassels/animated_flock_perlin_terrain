Shader "myShader"
{
    Properties
    {
        _OceanTex("Ocean Texture", 2D) = "white" {}
        _VegetationTex("Vegetation Texture", 2D) = "white" {}
        _BeachTex("Beach Texture", 2D) = "white" {}
        _SnowTex("Snow Texture", 2D) = "white" {}
        _Ka("Ambient Coefficient Ka", float) = 1
        _LightClr("Color of the light", vector) = (1, 1, 1, 1)
        _LightDir("Direction of lighting", vector) = (1, 0, 0, 1)
        _Glossiness ("Smoothness", Range(0,1)) = 1
        _Metallic ("Metallic", Range(0,1)) = 1
    }

        SubShader
        {
            Tags { "RenderType" = "Opaque"
            "LightMode" = "ForwardBase" }
            LOD 100

            Pass
            {
                CGPROGRAM
                #pragma vertex vert //2 shaders programs, vert and frag
                #pragma fragment frag

                #include "UnityCG.cginc"
                #include "Lighting.cginc"

                struct appdata
                {
                    float4 vertex : POSITION;
                    float4 texcoord : TEXCOORD0;
                    float3 normal : NORMAL;

                };

                struct v2f
                {
                    float4 uv : TEXCOORD0;
                    float height : TEXCOORD1;
                    float3 normal : TEXCOORD2;
                    float4 worldpos : TEXCOORD3;
                    float4 vertex : SV_POSITION;
                    bool isOcean : TEXCOORD4;
                    float linear_depth : TEXCOORD5;

                };
                
                sampler2D _OceanTex;
                sampler2D _VegetationTex;
                sampler2D _BeachTex;
                sampler2D _SnowTex;
                float _Ka;
                float4 _LightClr;
                float4 _LightDir;
                float4 _OceanTex_ST;

                v2f vert(appdata v)
                {
                    v2f o;
                    o.isOcean = false;
                    
                    //make waves in ocean
                    //I read the following tutorial for reference: https://catlikecoding.com/unity/tutorials/flow/waves/
                    float3 p = v.vertex.xyz;
                    if(p.y == 0) {
                        float k = 2 * UNITY_PI / 0.1;
                        float speed = 0.01f;
                        float amplitude = 0.04f;
                        float f_wave_1 = k * (p.x - (speed * _Time.y));
                        float f_wave_2 = k * (p.z - (speed * _Time.y));
                        p.y = amplitude * (sin(f_wave_1 + f_wave_2)); //I'm using x and z values so that the waves won't just be lines across the ocean -- I want ripples
                        float3 tangent = normalize(float3(1, k * amplitude * cos(f_wave_1 + f_wave_2), 0));
                        float3 normal = float3(-tangent.y, tangent.x, 0);
                        
                        v.normal = normal;
                        v.vertex.xyz = p;
                        o.isOcean = true;
                    }
                    
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.height = v.vertex.y;
                    o.normal = UnityObjectToWorldNormal(v.normal);
                    o.worldpos = mul(unity_ObjectToWorld, v.vertex);
                    o.uv = float4( v.texcoord.xy, 0, 0 );
                    o.linear_depth = o.vertex.w;
                    return o;
                }
                
                
                float3 convertRGBtoHSL(float3 colorRGB)
                {
                    float R = colorRGB.x;
                    float G = colorRGB.y;
                    float B = colorRGB.z;
                    
                    float M = max(R,max(G,B));
                    float m = min(R,min(G,B));
                    float C = M - m;
                    float L = 0.5f*(M+m);
                    
                    float Hprime;
                    
                    if(C==0) Hprime = 0;
                    else if(M==R) Hprime = ((G - B)/C)%6;
                    else if(M==G) Hprime = ((B - R)/C) + 2;
                    else if(M==B) Hprime = ((R - G)/C) + 4;
                    
                    float H = Hprime/6;
                    
                    float S;
                    if(L==1.0f) S = 0;
                    else S = C/(1 - (abs((2*L) - 1)));
                    
                    return float3(H,S,L);
                    
                }
                
                //math here is from https://www.reddit.com/r/gamedev/comments/4k8l33/using_an_hsl_shader_to_recolor_textures/
                float3 convertHSLtoRGB(float3 colorHSL)
                {
                    float H = colorHSL.x;
                    float S = colorHSL.y;
                    float L = colorHSL.z;
                    
                    float temp = H*6.0f;

                    float R = abs(temp - 3) - 1;
                    float G = 2.0 - abs(temp - 2);
                    float B = 2.0 - abs(temp - 4);
                    
                    float3 colorRGB = float3(R,G,B);
                    colorRGB = clamp(colorRGB, 0.0, 1.0); //puts the values between 0 and 1
                    
                    float C = (1 - abs((2*L) - 1)) * S;
                    return (colorRGB - 0.5) * C + L;
                }

                float4 frag(v2f i) : SV_Target //float4 is RGBA, colour for that fragment
                {   
                    // sample the texture as the albedo/basic fragment color. We choose which texture to use based on height
                    float3 fragment_color = tex2D(_SnowTex, i.uv).xyz; //default color is white
                    float height = i.height;
                    if (i.isOcean == true) fragment_color = tex2D(_OceanTex, i.uv).xyz;
                    else if (height < 0.05f) fragment_color = tex2D(_BeachTex, i.uv).xyz;
                    else if (height < 0.3f) fragment_color = tex2D(_VegetationTex, i.uv).xyz;
                    
                    //change saturation based on distance from camera
                    float3 hslColor = convertRGBtoHSL(fragment_color);
                    float saturation = 20.0f/pow(i.linear_depth,2); //adjust saturation according to linear depth
                    hslColor.y = saturation;
                    fragment_color = convertHSLtoRGB(hslColor);
                    
                    //Phong-Blinn reflection model
                    
                    float3 camera_position = _WorldSpaceCameraPos;
                    float3 this_position = i.worldpos.xyz;
                    

                    float3 n = normalize(i.normal);
                    float3 v =  normalize(camera_position - this_position);
                    float3 l = normalize(_WorldSpaceLightPos0.xyz); //light direction
                    
                    //ambient
                    //UNITY_LIGHTMODEL_AMBIENT = color of ambient light
                    float3 ambient = 0.3f * UNITY_LIGHTMODEL_AMBIENT.rgb * fragment_color;
                    
                    //diffuse
                    //don't want diffuse to be less than 0
                    float3 diffuse = fragment_color * max(0.0, dot(n, l));
                    
                    //specular
                    float3 specular;
                    if (dot(n, l) < 0.0) //light on other side
                    {
                        specular = float3(0.0, 0.0, 0.0);
                      }
                    else
                    {
                        float alpha = 20.0;
                        float3 r = reflect(-l,n); //r corresponds to shading lecture slides
                        //(1,1,1) = white = specular hightlight color
                        //don't want specular to be less than 0
                        specular = (1, 1, 1) * pow(max(0.0,dot(r, v)),alpha);
                    }

                    return float4(ambient + diffuse + specular, 1);
                }
                ENDCG
            }
        }
}
