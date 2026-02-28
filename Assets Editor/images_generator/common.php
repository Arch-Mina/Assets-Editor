<?php
	// prevent direct access to this file
	if (basename(__FILE__) == basename($_SERVER['SCRIPT_FILENAME'])) {
		header('Location: /');
		exit;
	}

	// load config.php from current directory
	require_once(__DIR__ . '/config.php');

	// cache duration: 1 month
	header('Cache-control: max-age=' . (60 * 60 * 1 * 365));
	header('Expires: ' . gmdate(DATE_RFC1123, time() + 60 * 60 * 1 * 365));

	// serve the file by name
	function serveFile($fileName, $type) {
		header('Last-Modified: ' . gmdate('D, d M Y H:i:s', filemtime($fileName)) . ' GMT');
		if (isset($_SERVER['HTTP_IF_MODIFIED_SINCE'])) {
			header('HTTP/1.0 304 Not Modified');
			header('Cache-Control: public');
			header('Pragma: cache');
			exit;
		}
		header('Content-Type: image/'.$type);
		readfile($fileName);
	}

	// Block sites that hotlink your host and overload it
	// see config.php for configuration
	function is_abuser($referer) {
		global $abusersList;
		return in_array(parse_url($referer, PHP_URL_HOST), $abusersList)
			|| in_array(substr(parse_url($referer, PHP_URL_HOST), 4), $abusersList);
	}

	// helper to read a number from GET parameter
	function getURLArg($key) {
		return isset($_GET[$key]) && is_numeric($_GET[$key]) ? (int)$_GET[$key] : 0;
	}
?>
