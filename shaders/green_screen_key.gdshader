shader_type canvas_item;

// 目标抠像颜色（默认纯绿）。
uniform vec4 key_color : source_color = vec4(0.0, 1.0, 0.0, 1.0);
// 抠像灵敏度，越大越容易把近绿区域扣掉。
uniform float threshold : hint_range(0.0, 1.0, 0.001) = 0.35;
// 边缘过渡宽度，越大越柔和。
uniform float softness : hint_range(0.001, 1.0, 0.001) = 0.08;

void fragment() {
	vec4 src = texture(TEXTURE, UV);

	float dist = distance(src.rgb, key_color.rgb);
	float edge = max(softness, 0.0001);

	// 使用 smoothstep 平滑过渡：接近 key_color 的像素逐步变透明。
	float keep = smoothstep(threshold - edge, threshold + edge, dist);

	COLOR.rgb = src.rgb;
	COLOR.a = src.a * keep;
}
