<?php
// prevent direct access to this file
if (basename(__FILE__) == basename($_SERVER['SCRIPT_FILENAME'])) {
	header('Location: /');
	exit;
}

// list of domains blacklisted from your image service
$abusersList = array('domain1.example.com', 'bad-server.com');

// outfit - image sizes
// size=1 may be useful for displaying mounted characters
$imageSizes = [
	0 => 64,
	1 => 96
];

/*
some server configurations may print warnings from graphics library
these warnings are interpreted as part of image and make image unreadable for web browsers
if you get black/empty image, you can try to uncomment line below to disable all warnings
*/
// error_reporting(0);
