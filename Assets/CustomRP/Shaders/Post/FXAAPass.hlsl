#ifndef CUSTOM_FXAA_PASS_INCLUDED
#define CUSTOM_FXAA_PASS_INCLUDED

#if defined(FXAA_QUALITY_LOW)
	#define EXTRA_EDGE_STEPS 3
	#define EDGE_STEP_SIZES 1.5, 2.0, 2.0
	#define LAST_EDGE_STEP_GUESS 8.0
#elif defined(FXAA_QUALITY_MEDIUM)
	#define EXTRA_EDGE_STEPS 8
	#define EDGE_STEP_SIZES 1.5, 2.0, 2.0, 2.0, 2.0, 2.0, 2.0, 4.0
	#define LAST_EDGE_STEP_GUESS 8.0
#else
	#define EXTRA_EDGE_STEPS 10
	#define EDGE_STEP_SIZES 1.0, 1.0, 1.0, 1.0, 1.5, 2.0, 2.0, 2.0, 2.0, 4.0
	#define LAST_EDGE_STEP_GUESS 8.0
#endif

static const float edgeStepSizes[EXTRA_EDGE_STEPS] = { EDGE_STEP_SIZES };

float4 _FXAAConfig;

struct FXAAEdge
{
    bool isHorizontal;
    float pixelStep;
    float lumaGradient, otherLuma;
};

struct LumaNeighborhood
{
    float m, n, e, s, w, ne, se, sw, nw;
    float highest, lowest, range;
};


float GetLuma(float2 uv, float uOffset = 0.0, float vOffset = 0.0)
{
    uv += float2(uOffset, vOffset) * GetSourceTexelSize().xy;
    
    //gamama-adjusted 2.2,this case get a approximate value is 2
    //return sqrt(Luminance(GetSource(uv)));
    
    //directly use the green color channel that avoids a dot product and square root operation
#if defined(FXAA_ALPHA_CONTAINS_LUMA)
		return GetSource(uv).a;
#else
    return GetSource(uv).g;
#endif
}

bool IsHorizontalEdge(LumaNeighborhood luma)
{
    float horizontal =
		2.0 * abs(luma.n + luma.s - 2.0 * luma.m) +
		abs(luma.ne + luma.se - 2.0 * luma.e) +
		abs(luma.nw + luma.sw - 2.0 * luma.w);
    float vertical =
		2.0 * abs(luma.e + luma.w - 2.0 * luma.m) +
		abs(luma.ne + luma.nw - 2.0 * luma.n) +
		abs(luma.se + luma.sw - 2.0 * luma.s);
    return horizontal >= vertical;
}

LumaNeighborhood GetLumaNeighborhood(float2 uv)
{
    LumaNeighborhood luma;
    luma.m = GetLuma(uv);
    luma.n = GetLuma(uv, 0.0, 1.0);
    luma.e = GetLuma(uv, 1.0, 0.0);
    luma.s = GetLuma(uv, 0.0, -1.0);
    luma.w = GetLuma(uv, -1.0, 0.0);
    luma.ne = GetLuma(uv, 1.0, 1.0);
    luma.se = GetLuma(uv, 1.0, -1.0);
    luma.sw = GetLuma(uv, -1.0, -1.0);
    luma.nw = GetLuma(uv, -1.0, 1.0);
    luma.highest = max(max(max(max(luma.m, luma.n), luma.e), luma.s), luma.w);
    luma.lowest = min(min(min(min(luma.m, luma.n), luma.e), luma.s), luma.w);
    luma.range = luma.highest - luma.lowest;
    return luma;
}

bool CanSkipFXAA(LumaNeighborhood luma)
{
    return luma.range < max(_FXAAConfig.x, _FXAAConfig.y * luma.highest);
}

FXAAEdge GetFXAAEdge(LumaNeighborhood luma)
{
    FXAAEdge edge;
    edge.isHorizontal = IsHorizontalEdge(luma);
    
    float lumaP, lumaN;
    if (edge.isHorizontal)
    {
        edge.pixelStep = GetSourceTexelSize().y;
        lumaP = luma.n;
        lumaN = luma.s;
    }
    else
    {
        edge.pixelStep = GetSourceTexelSize().x;
        lumaP = luma.e;
        lumaN = luma.w;
    }
    
    float gradientP = abs(lumaP - luma.m);
    float gradientN = abs(lumaN - luma.m);

    //north and east is positive
    //这里计算 positive and negative 的 对比度，以此来决定 pixelStep的正负
    if (gradientP < gradientN)
    {
        edge.pixelStep = -edge.pixelStep;
        edge.lumaGradient = gradientN;
        edge.otherLuma = lumaN;
    }
    else{
        
        edge.lumaGradient = gradientP;
        edge.otherLuma = lumaP;
    }
    
    return edge;
}

float GetSubpixelBlendFactor(LumaNeighborhood luma)
{
    float filter = 2.0 * (luma.n + luma.e + luma.s + luma.w);
    filter += luma.ne + luma.nw + luma.se + luma.sw;
    filter *= 1.0 / 12.0;
    filter = saturate(filter / luma.range);
    filter = smoothstep(0, 1, filter);
    return filter * filter * _FXAAConfig.z;
}

float GetEdgeBlendFactor(LumaNeighborhood luma, FXAAEdge edge, float2 uv)
{
    float2 edgeUV = uv;
    float2 uvStep = 0.0;
    if (edge.isHorizontal)
    {
        edgeUV.y += 0.5 * edge.pixelStep;
        uvStep.x = GetSourceTexelSize().x;
    }
    else
    {
        edgeUV.x += 0.5 * edge.pixelStep;
        uvStep.y = GetSourceTexelSize().y;
    }
    
    //与混合反方向上邻居求均值
    float edgeLuma = 0.5 * (luma.m + edge.otherLuma);
    //FXAA uses a quarter of the luma gradient of the edge as the threshold for this check
    float gradientThreshold = 0.25 * edge.lumaGradient;
    
    //direction of positive 
    float2 uvP = edgeUV + uvStep;
    float lumaDeltaP = GetLuma(uvP) - edgeLuma;
    bool atEndP = abs(lumaDeltaP) >= gradientThreshold;
	
    UNITY_UNROLL
    for (int i = 0; i < EXTRA_EDGE_STEPS && !atEndP; i++)
    {
        uvP += uvStep * edgeStepSizes[i];
        lumaDeltaP = GetLuma(uvP) - edgeLuma;
        atEndP = abs(lumaDeltaP) >= gradientThreshold;
    }
    if (!atEndP){
        uvP += uvStep * LAST_EDGE_STEP_GUESS;
    }
    //direction of nagative
    float2 uvN = edgeUV - uvStep;
    float lumaDeltaN = GetLuma(uvN) - edgeLuma;
    bool atEndN = abs(lumaDeltaN) >= gradientThreshold;

    UNITY_UNROLL
    for (int j = 0; j < EXTRA_EDGE_STEPS && !atEndN; j++)
    {
        uvN -= uvStep * edgeStepSizes[i];
        lumaDeltaN = GetLuma(uvN) - edgeLuma;
        atEndN = abs(lumaDeltaN) >= gradientThreshold;
    }
    if (!atEndN){
        uvN -= uvStep * LAST_EDGE_STEP_GUESS;
    }
    float distanceToEndP, distanceToEndN;
    if (edge.isHorizontal)
    {
        distanceToEndP = uvP.x - uv.x;
        distanceToEndN = uv.x - uvN.x;
    }
    else
    {
        distanceToEndP = uvP.y - uv.y;
        distanceToEndN = uv.y - uvN.y;
    }

    float distanceToNearestEnd;
    bool deltaSign;
    if (distanceToEndP <= distanceToEndN)
    {
        distanceToNearestEnd = distanceToEndP;
        deltaSign = lumaDeltaP >= 0;
    }
    else
    {
        distanceToNearestEnd = distanceToEndN;
        deltaSign = lumaDeltaN >= 0;
    }

    if (deltaSign == (luma.m - edgeLuma >= 0)){
        return 0.0;
    }
    else{
        return 0.5 - distanceToNearestEnd / (distanceToEndP + distanceToEndN);
    }
}

float4 FXAAPassFragment(Varyings input) : SV_TARGET
{
    LumaNeighborhood luma = GetLumaNeighborhood(input.screenUV);
    if (CanSkipFXAA(luma))
    {
        return GetSource(input.screenUV);
    }
    
    FXAAEdge edge = GetFXAAEdge(luma);
    float blendFactor = max(GetSubpixelBlendFactor(luma), GetEdgeBlendFactor(luma, edge, input.screenUV));

    float2 blendUV = input.screenUV;
    if (edge.isHorizontal)
    {
        blendUV.y += blendFactor * edge.pixelStep;
    }
    else
    {
        blendUV.x += blendFactor * edge.pixelStep;
    }

    return GetSource(blendUV);
}

#endif