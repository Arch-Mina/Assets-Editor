<?php
	// load functions from current directory
	require_once(__DIR__ . '/common.php');

	// do not generate images for blacklisted addresses
	if (isset($_SERVER['HTTP_REFERER']) && is_abuser($_SERVER['HTTP_REFERER'])) {
		serveFile('abuse_warning.png', 'png');
		exit;
	}

	$id = getURLArg('id');

	$directory = __DIR__ . '/items/' . $id;
	if (!is_dir($directory)) {
		serveFile('no_image.png', 'png');
		exit;
	}

	// example:
	// item.php?id=675&mode=loot // small enchanted sapphire (wiki/loot table mode)
	if (isset($_GET['mode']) && $_GET['mode'] === "loot") {
		$fileName = $directory . '/loot.gif';
		if (file_exists($fileName)) {
			serveFile($fileName, 'gif');
			exit;
		}
	}

	// not set to loot mode or loot.gif was not found, see if count/subtype was declared
	// example:
	// item.php?id=9045&count=5 // 5x gold ingot
	// item.php?id=2873&subtype=1 // bucket of water
	$frame = 0;
	if (isset($_GET['count'])) {
		$count = getURLArg('count');
		switch (true) {
			case ($count >= 0 && $count <= 1):
				$frame = 0;
				break;
			case ($count == 2):
				$frame = 1;
				break;
			case ($count == 3):
				$frame = 2;
				break;
			case ($count == 4):
				$frame = 3;
				break;
			case ($count >= 5 && $count <= 9):
				$frame = 4;
				break;
			case ($count >= 10 && $count <= 24):
				$frame = 5;
				break;
			case ($count >= 25 && $count <= 49):
				$frame = 6;
				break;
			case ($count >= 50):
				$frame = 7;
				break;
		}
	} elseif (isset($_GET['subtype'])) {
		$frame = getURLArg('subtype');
	}

	// serve exact count/subtype
	$fileName = $directory . '/' . $frame . '.gif';
	if (file_exists($fileName)) {
		serveFile($fileName, 'gif');
		exit;
	}

	// count/subtype was not found
	// example situation: item.php?id=2882&subtype=10
	// jug of water - fluid container with no own frame for subtype
	// fallback to 0.gif
	$frame = 0;
	$fileName = $directory . '/' . $frame . '.gif';
	if (file_exists($fileName)) {
		serveFile($fileName, 'gif');
		exit;
	}

	// directory did not have any suitable image
	// return error image
	serveFile('no_image.png', 'png');
	exit;
?>
