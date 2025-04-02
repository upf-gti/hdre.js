
varying vec3 v_position;
varying vec3 v_world_position;
varying vec3 v_normal;
varying vec2 v_uv;
varying vec4 v_color;

uniform float u_exposure;
uniform vec4 u_color;
uniform samplerCube u_texture;
uniform vec3 u_camera_position;

// Uncharted 2 tone map
// see: http://filmicworlds.com/blog/filmic-tonemapping-operators/
vec3 toneMapUncharted2Impl(vec3 color)
{
    const float A = 0.15;
    const float B = 0.50;
    const float C = 0.10;
    const float D = 0.20;
    const float E = 0.02;
    const float F = 0.30;
    return ((color*(A*color+C*B)+D*E)/(color*(A*color+B)+D*F))-E/F;
}

vec3 toneMapUncharted(vec3 color)
{
    const float W = 11.2;
    color = toneMapUncharted2Impl(color * 2.0);
    vec3 whiteScale = 1.0 / toneMapUncharted2Impl(vec3(W));
    return color * whiteScale;//LINEARtoSRGB(color * whiteScale);
}

void main()
{
	vec3 E = normalize(u_camera_position - v_world_position);
	vec4 color = u_color * textureCube( u_texture, E );

	color = pow(color, vec4(1.0/2.2));

	// uncharted 
	color.rgb = toneMapUncharted(color.rgb) * u_exposure;

	gl_FragColor = color;
}
