shader_type spatial;

uniform vec3 color_a : source_color = vec3(0.4);
uniform vec3 color_b : source_color = vec3(0.6);
uniform float tile_size = 1.0;

varying vec3 world_pos;

void vertex() {
    world_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
}

void fragment() {
    vec2 p = world_pos.xz / tile_size;
    float checker = mod(floor(p.x) + floor(p.y), 2.0);

    ALBEDO = mix(color_a, color_b, checker);
    METALLIC = 0.0;
    ROUGHNESS = 0.95;
}
