#ifndef URP_VOLUMETRIC_CLOUDS_UPSCALE_HLSL
#define URP_VOLUMETRIC_CLOUDS_UPSCALE_HLSL

float _CloudOutlineStrength;
float _CloudOutlineWidth;
float4 _CloudOutlineColor;

half4 ApplyCloudContour(float2 uv, half4 cloud)
{
    if (_CloudOutlineStrength <= 0.0) return cloud;
    float2 delta = _CloudOutlineWidth / _ScreenParams.xy;
    half4 left = SAMPLE_TEXTURE2D_X_LOD(_VolumetricCloudsLightingTexture,s_linear_clamp_sampler,uv-float2(delta.x,0),0);
    half4 right = SAMPLE_TEXTURE2D_X_LOD(_VolumetricCloudsLightingTexture,s_linear_clamp_sampler,uv+float2(delta.x,0),0);
    half4 down = SAMPLE_TEXTURE2D_X_LOD(_VolumetricCloudsLightingTexture,s_linear_clamp_sampler,uv-float2(0,delta.y),0);
    half4 up = SAMPLE_TEXTURE2D_X_LOD(_VolumetricCloudsLightingTexture,s_linear_clamp_sampler,uv+float2(0,delta.y),0);
    half opacity = saturate(1.0h-cloud.a);
    half densityEdge = max(abs(left.a-right.a),abs(up.a-down.a));
    half lightEdge = max(length(left.rgb-right.rgb),length(up.rgb-down.rgb));
    half contour = smoothstep(0.008h,0.045h,max(densityEdge,lightEdge*0.4h));
    cloud.rgb = lerp(cloud.rgb,_CloudOutlineColor.rgb*opacity,contour*_CloudOutlineStrength*opacity);
    return cloud;
}

// Bilateral kernel size
#define KERNEL_SIZE 3

// Avoid division by zero
#define _UpsampleTolerance 1e-5
//#define _NoiseFilterStrength 0.99999999

half Weight(half distance)
{
    return exp(-distance * distance);
}

half4 BilateralUpscale(float2 screenUV)
{
    // Calculate the current screenUV in a low resolution texture.
    float2 offsetUV = (floor(screenUV * _VolumetricCloudsLightingTexture_TexelSize.zw) + 0.5) * _VolumetricCloudsLightingTexture_TexelSize.xy;
    half4 centerColor = SAMPLE_TEXTURE2D_X_LOD(_VolumetricCloudsLightingTexture, s_linear_clamp_sampler, screenUV, 0).rgba;

    half4 resultColor = half4(0.0, 0.0, 0.0, 0.0);
    half normalization = 0.0;

    for (int i = -KERNEL_SIZE; i <= KERNEL_SIZE; i++)
    {
        for (int j = -KERNEL_SIZE; j <= KERNEL_SIZE; j++)
        {
            half4 neighborColor = SAMPLE_TEXTURE2D_X_LOD(_VolumetricCloudsLightingTexture, s_linear_clamp_sampler, offsetUV + float2(i, j) * _VolumetricCloudsLightingTexture_TexelSize.xy, 0).rgba;

            half2 distance = (screenUV - offsetUV) * _ScreenParams.xy;

            half colorDiff = length(centerColor.rgba - neighborColor.rgba);

            half weight = Weight(length(distance)) * rcp(colorDiff + _UpsampleTolerance);

            resultColor += neighborColor * weight;
            normalization += weight;
        }
    }
    
    resultColor *= rcp(normalization);

    return resultColor;
}

half BilateralUpscaleTransmittance(float2 screenUV)
{
    // Calculate the current screenUV in a low resolution texture.
    float2 offsetUV = (floor(screenUV * _VolumetricCloudsLightingTexture_TexelSize.zw) + 0.5) * _VolumetricCloudsLightingTexture_TexelSize.xy;
    half centerTransmittance = SAMPLE_TEXTURE2D_X_LOD(_VolumetricCloudsLightingTexture, s_linear_clamp_sampler, screenUV, 0).a;

    half resultTransmittance = 0.0;
    half normalization = 0.0;

    for (int i = -KERNEL_SIZE; i <= KERNEL_SIZE; i++)
    {
        for (int j = -KERNEL_SIZE; j <= KERNEL_SIZE; j++)
        {
            half neighborTransmittance = SAMPLE_TEXTURE2D_X_LOD(_VolumetricCloudsLightingTexture, s_linear_clamp_sampler, offsetUV + float2(i, j) * _VolumetricCloudsLightingTexture_TexelSize.xy, 0).a;

            half2 distance = (screenUV - offsetUV) * _ScreenParams.xy;

            half transmittanceDiff = abs(centerTransmittance - neighborTransmittance);

            half weight = Weight(length(distance)) * rcp(transmittanceDiff + _UpsampleTolerance);

            resultTransmittance += neighborTransmittance * weight;
            normalization += weight;
        }
    }

    resultTransmittance *= rcp(normalization);

    return resultTransmittance;
}

#endif
