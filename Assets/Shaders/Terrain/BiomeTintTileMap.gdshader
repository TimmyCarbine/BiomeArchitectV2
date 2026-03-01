shader_type canvas_item;

uniform sampler2D u_palette_tex;
uniform float u_palette_size;

uniform sampler2D u_biome_index_tex;

uniform vec2 u_window_origin_tile;
uniform vec2 u_window_size_tile;
uniform vec2 u_tile_size_px;

uniform vec2 u_view_origin_px;
uniform vec2 u_tilemap_origin_px; // NEW

void fragment()
{
    vec4 base = texture(TEXTURE, UV);

    // screen pixel -> world pixel
    vec2 world_px = u_view_origin_px + FRAGCOORD.xy;

    // world pixel -> tilemap-local pixel
    vec2 map_px = world_px - u_tilemap_origin_px;

    // tilemap-local pixel -> tile coordinate
    ivec2 tile = ivec2(floor(map_px / u_tile_size_px));
    ivec2 local_tile = tile - ivec2(u_window_origin_tile);

    bool inside =
        local_tile.x >= 0 && local_tile.y >= 0 &&
        local_tile.x < int(u_window_size_tile.x) &&
        local_tile.y < int(u_window_size_tile.y);

    vec3 tint = vec3(1.0);

    if (inside)
    {
        float r = texelFetch(u_biome_index_tex, local_tile, 0).r;
        float idx = floor(r * 255.0 + 0.5);

        if (idx < 255.0)
        {
            float palette_u = (idx + 0.5) / u_palette_size;
            tint = texture(u_palette_tex, vec2(palette_u, 0.5)).rgb;
        }
    }

    COLOR = vec4(base.rgb * tint, base.a);
}