<?php
	// example with all parameters:
	// outfit.php?id=128&head=10&body=20&legs=30&feet=40&addons=3&mount=1363&mounthead=50&mountbody=60&mountlegs=70&mountfeet=80&direction=2&animated=1&walk=1&size=1

	// animated is separate from walk because of specific cases like rotworm and thundergiant

	// "animated" parameter values:
	// 0 or not set - single frame
	// 1 - animated

	// "walk" parameter values:
	// 0 or not set - idle animation
	// 1 - walk animation
	// 2 - rotating idle
	// 3 - rotating walking

	// "size" parameter values:
	// 0 - serve 64x64
	// 1 - serve 96x96

	require_once(__DIR__ . '/common.php');

	// do not generate images for blacklisted addresses
	if (isset($_SERVER['HTTP_REFERER']) && is_abuser($_SERVER['HTTP_REFERER'])) {
		serveFile('abuse_warning.png', 'png');
		exit;
	}

	// lookTypeEx was provided
	// serve an item instead of outfit
	$lookTypeEx = getURLArg('typeex');
	if ($lookTypeEx != 0) {
		$_GET['id'] = $lookTypeEx;
		require_once(__DIR__ . '/item.php');
		exit;
	}

	require_once(__DIR__ . '/outfit_generator.php');

	if (!function_exists('gd_info')) {
		die("GD extension is required for image processing! Search for \"extension=gd\" in your php.ini.<br />Remember to restart or reload your web server.");
	}

	$lookType = getURLArg('type');
	if ($lookType == 0) {
		// compatibility with old outfit url generators
		$lookType = getURLArg('id');
	}

	$colors = [ getURLArg('head'), getURLArg('body'), getURLArg('legs'), getURLArg('feet') ];
	$addons = getURLArg('addons');
	$mount = getURLArg('mount');
	$mountColors = [ getURLArg('mounthead'), getURLArg('mountbody'), getURLArg('mountlegs'), getURLArg('mountfeet') ];
	$direction = getURLArg('direction');
	$animated = getURLArg('animated'); // 
	$walk = getURLArg('walk');
	$size = getURLArg('size');
	Outfit::getInstance()->Serve($lookType, $colors, $addons, $mount, $mountColors, $direction, $animated, $walk, $size);
?>
