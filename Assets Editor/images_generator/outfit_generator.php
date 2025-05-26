<?php
	// prevent direct access to this file
	if (basename(__FILE__) == basename($_SERVER['SCRIPT_FILENAME'])) {
		header('Location: /');
		exit;
	}

	// include gif creator libraries
	require(__DIR__ . '/gifCreator.php');

	// outfit animation speeds
	// this is to calibrate synchronization for animations
	// 1 = 10ms
	$walkSpeeds = [
		1 => 72,
		2 => 36,
		3 => 24,
		4 => 18,
		5 => 14,
		6 => 12,
		7 => 10,
		8 => 9,
		9 => 8
	];

	// outfit generator class
	class Outfit {
		// singleton
		private static $instance = null;
		public static function getinstance() {
			if (!isset(self::$instance))
				self::$instance = new self();
			return self::$instance;
		}

		// outfit color palette
		private static $colorPalette = array(
			0xFFFFFF, 0xFFD4BF, 0xFFE9BF, 0xFFFFBF, 0xE9FFBF, 0xD4FFBF,
			0xBFFFBF, 0xBFFFD4, 0xBFFFE9, 0xBFFFFF, 0xBFE9FF, 0xBFD4FF,
			0xBFBFFF, 0xD4BFFF, 0xE9BFFF, 0xFFBFFF, 0xFFBFE9, 0xFFBFD4,
			0xFFBFBF, 0xDADADA, 0xBF9F8F, 0xBFAF8F, 0xBFBF8F, 0xAFBF8F,
			0x9FBF8F, 0x8FBF8F, 0x8FBF9F, 0x8FBFAF, 0x8FBFBF, 0x8FAFBF,
			0x8F9FBF, 0x8F8FBF, 0x9F8FBF, 0xAF8FBF, 0xBF8FBF, 0xBF8FAF,
			0xBF8F9F, 0xBF8F8F, 0xB6B6B6, 0xBF7F5F, 0xBFAF8F, 0xBFBF5F,
			0x9FBF5F, 0x7FBF5F, 0x5FBF5F, 0x5FBF7F, 0x5FBF9F, 0x5FBFBF,
			0x5F9FBF, 0x5F7FBF, 0x5F5FBF, 0x7F5FBF, 0x9F5FBF, 0xBF5FBF,
			0xBF5F9F, 0xBF5F7F, 0xBF5F5F, 0x919191, 0xBF6A3F, 0xBF943F,
			0xBFBF3F, 0x94BF3F, 0x6ABF3F, 0x3FBF3F, 0x3FBF6A, 0x3FBF94,
			0x3FBFBF, 0x3F94BF, 0x3F6ABF, 0x3F3FBF, 0x6A3FBF, 0x943FBF,
			0xBF3FBF, 0xBF3F94, 0xBF3F6A, 0xBF3F3F, 0x6D6D6D, 0xFF5500,
			0xFFAA00, 0xFFFF00, 0xAAFF00, 0x54FF00, 0x00FF00, 0x00FF54,
			0x00FFAA, 0x00FFFF, 0x00A9FF, 0x0055FF, 0x0000FF, 0x5500FF,
			0xA900FF, 0xFE00FF, 0xFF00AA, 0xFF0055, 0xFF0000, 0x484848,
			0xBF3F00, 0xBF7F00, 0xBFBF00, 0x7FBF00, 0x3FBF00, 0x00BF00,
			0x00BF3F, 0x00BF7F, 0x00BFBF, 0x007FBF, 0x003FBF, 0x0000BF,
			0x3F00BF, 0x7F00BF, 0xBF00BF, 0xBF007F, 0xBF003F, 0xBF0000,
			0x242424, 0x7F2A00, 0x7F5500, 0x7F7F00, 0x557F00, 0x2A7F00,
			0x007F00, 0x007F2A, 0x007F55, 0x007F7F, 0x00547F, 0x002A7F,
			0x00007F, 0x2A007F, 0x54007F, 0x7F007F, 0x7F0055, 0x7F002A,
			0x7F0000,
		);

		private static $transparentBackgroundColor = array(255, 255, 255);
		private const ERRIMAGE = 'no_image.png'; // default image when outfit cannot be displayed

		// generates filename to search in
		private function GetFrameFileName($frameGroup, $mountedState, $addonLayer, $direction, $animationPhase, $isTemplate) {
			$template = $isTemplate ? '_template' : '';
			return "{$frameGroup}_{$addonLayer}_{$mountedState}_{$direction}_{$animationPhase}{$template}.png";
		}

		private function GetOutfitFiles($lookType, $direction, $frameGroup, $addonFlags, $mountedState) {
			// direction on image files is indexed from 1
			++$direction;

			$outfitDir = __DIR__ . '/outfits/' . $lookType . '/';
			$frames = [
				"base"      => [ ],
				"base_t"    => [ ],
				"addon_1"   => [ ],
				"addon_1_t" => [ ],
				"addon_2"   => [ ],
				"addon_2_t" => [ ],
			];

			$animationPhase = 1;

			// determine the animation type
			$fileName = $this->GetFrameFileName($frameGroup, 1, 1, $direction, $animationPhase, false);
			if (!file_exists($outfitDir . $fileName)) {
				// missing walk/idle animation, use other one instead
				// possible examples: rotworm, old citizen with no movement animation
				$frameGroup = ($frameGroup + 1) % 2;
				$fileName = $this->GetFrameFileName($frameGroup, 1, 1, $direction, $animationPhase, false);
				if (!file_exists($outfitDir . $fileName)) {
					// final fallback - check if frame 1 exists
					$fileName = $this->GetFrameFileName(1, 1, 1, 1, 1, false);
					if (file_exists($outfitDir . $fileName)) {
						// very specific case - looktype 1
						$frames['base'][] = $outfitDir . $fileName;
						return $frames;
					} else {
						// the outfit has no suitable images to display
						return false;
					}
				}
			}

			// determine if mounted state can be used
			if ($mountedState != 1) {
				$fileName = $this->GetFrameFileName($frameGroup, $mountedState, 1, $direction, $animationPhase, false);
				if (!file_exists($outfitDir . $fileName)) {
					// the outfit does not have a mounted version
					// use dismounted one
					$mountedState = 1;
				}
			}

			// load frames
			while (true) {
				// BASE OUTFIT ANIMATION FRAME
				$fileName = $this->GetFrameFileName($frameGroup, $mountedState, 1, $direction, $animationPhase, false);
				if (file_exists($outfitDir . $fileName)) {
					$frames['base'][] = $outfitDir . $fileName;
				} else {
					break; // no more frames
				}

				// BASE OUTFIT COLOR MASK
				$fileName = $this->GetFrameFileName($frameGroup, $mountedState, 1, $direction, $animationPhase, true);
				if (file_exists($outfitDir . $fileName)) {
					$frames['base_t'][] = $outfitDir . $fileName;
				}

				// ADDONS
				for ($addonLayer = 1; $addonLayer <= 2; ++$addonLayer) {
					// check if this addon is used in the requested outfit
					if (($addonFlags & (1 << ($addonLayer - 1))) === 0) {
						continue;
					}

					// ADDON ANIMATION FRAME
					$fileName = $this->GetFrameFileName($frameGroup, $mountedState, $addonLayer + 1, $direction, $animationPhase, false);
					if (file_exists($outfitDir . $fileName)) {
						$frames["addon_{$addonLayer}"][] = $outfitDir . $fileName;
					}

					// ADDON COLOR MASK
					$fileName = $this->GetFrameFileName($frameGroup, $mountedState, $addonLayer + 1, $direction, $animationPhase, true);
					if (file_exists($outfitDir . $fileName)) {
						$frames["addon_{$addonLayer}_t"][] = $outfitDir . $fileName;
					}
				}

				// move to next frame
				++$animationPhase;
			}

			return $frames;
		}

		// serve an outfit according to provided details
		public function Serve($lookType, $colors, $addons, $mount, $mountColors, $direction, $animated, $walk, $size) {
			if (!is_dir(__DIR__ . '/outfits/' . $lookType)) {
				serveFile(self::ERRIMAGE, 'png');
				exit;
			}

			// determine if mounted version of outfit should be used
			$mountedState = $mount == 0 ? 1 : 2;

			// determine which framegroup to use
			$frameGroup = $walk % 2;

			// total GIF frames and their durations
			$frames = [];
			$durations = [];

			$currentDirection = $direction;
			$directionCount = $walk > 1 ? 4 : 1;

			for ($i = 0; $i < $directionCount; ++$i) {
				// get base outfit files to build from
				$outfitFiles = $this->GetOutfitFiles($lookType, $direction % 4, $frameGroup, $addons, $mountedState);
				if ($outfitFiles === false) {
					serveFile(self::ERRIMAGE, 'png');
					exit;
				}

				if ($mount != 0) {
					$mountFiles = $this->GetOutfitFiles($mount, $direction % 4, $frameGroup, 0, 0);
				}

				// get frame durations for both outfit and mount
				$outfitFrameCount = count($outfitFiles['base']);
				$mountFrameCount = isset($mountFiles) ? count($mountFiles['base']) : 1;

				// in the "new" outfits, the last frame is identical to first
				// this code skips the duplicated frame
				// making the animations smoother
				if ($outfitFrameCount > 3) --$outfitFrameCount;
				if ($mountFrameCount > 3) --$mountFrameCount;

				global $walkSpeeds;
				$frameDurationOutfit = $walkSpeeds[$outfitFrameCount] ?? 10;
				if (isset($mountFiles)) {
					$frameDurationMount = $walkSpeeds[$mountFrameCount] ?? 10;
				}

				// total animation time
				$totalDuration = $frameDurationOutfit * $outfitFrameCount;

				// lowest frame duration
				$frameDuration = isset($mountFiles) ? min($frameDurationOutfit, $frameDurationMount) : $frameDurationOutfit;

				// build the animation
				$currentDuration = 0;

				global $imageSizes;
				$imageSize = isset($imageSizes[$size]) ? $imageSizes[$size] : 64;

				while ($currentDuration < $totalDuration) {
					// single frame only
					if ($animated == 0) {
						$frameDuration = $totalDuration;
					}

					// base layer
					$image_outfit = imagecreatetruecolor($imageSize, $imageSize);
					$transparentColor = imagecolorallocate($image_outfit, self::$transparentBackgroundColor[0], self::$transparentBackgroundColor[1], self::$transparentBackgroundColor[2]);
					imagefill($image_outfit, 0, 0, $transparentColor);
					imagecolortransparent($image_outfit, $transparentColor);
					imagealphablending($image_outfit, false);
					imagesavealpha($image_outfit, true);

					// mask layer
					$image_colorMask = imagecreatetruecolor($imageSize, $imageSize);
					$bgcolor = imagecolorallocate($image_colorMask, self::$transparentBackgroundColor[0], self::$transparentBackgroundColor[1], self::$transparentBackgroundColor[2]);
					imagecolortransparent($image_colorMask, $bgcolor);
					imagealphablending($image_colorMask, false);
					imagesavealpha($image_colorMask, true);

					// mount
					if (isset($mountFiles)) {
						// CURRENT MOUNT FRAME
						$mountIndex = floor(($currentDuration / $frameDurationMount)) % $mountFrameCount;

						// BASE MOUNT LAYER
						$layer = imagecreatefrompng($mountFiles['base'][$mountIndex]);
						$this->alphaOverlay($image_outfit, $layer);
						imagedestroy($layer);

						// MOUNT COLORS
						if (isset($mountFiles['base_t'][$mountIndex])) {
							$colorMask = imagecreatefrompng($mountFiles['base_t'][$mountIndex]);
							$this->alphaOverlay($image_colorMask, $colorMask);
							imagedestroy($colorMask);
						}

						// apply mount colors and reset color mask
						$this->colorize($image_colorMask, $image_outfit, $mountColors[0], $mountColors[1], $mountColors[2], $mountColors[3]);
						imagedestroy($image_colorMask);
						$image_colorMask = imagecreatetruecolor($imageSize, $imageSize);
						$bgcolor = imagecolorallocate($image_colorMask, self::$transparentBackgroundColor[0], self::$transparentBackgroundColor[1], self::$transparentBackgroundColor[2]);
						imagecolortransparent($image_colorMask, $bgcolor);
						imagealphablending($image_colorMask, false);
						imagesavealpha($image_colorMask, true);
					}

					// CURRENT OUTFIT FRAME
					$outfitIndex = floor(($currentDuration / $frameDurationOutfit)) % $outfitFrameCount;

					// BASE OUTFIT LAYER
					$tmpOutfit = imagecreatefrompng($outfitFiles['base'][$outfitIndex]);
					$this->alphaOverlay($image_outfit, $tmpOutfit);
					imagedestroy($tmpOutfit);

					// BASE OUTFIT COLORS
					if (isset($outfitFiles['base_t'][$outfitIndex])) {
						$colorMask = imagecreatefrompng($outfitFiles['base_t'][$outfitIndex]);
						$this->alphaOverlay($image_colorMask, $colorMask);
						imagedestroy($colorMask);
					}

					// ADDONS LAYER
					for ($addonLayer = 1; $addonLayer <= 2; ++$addonLayer) {
						if (isset($outfitFiles["addon_{$addonLayer}"][$outfitIndex])) {
							$addonImage = imagecreatefrompng($outfitFiles["addon_{$addonLayer}"][$outfitIndex]);
							$this->alphaOverlay($image_outfit, $addonImage);
							imagedestroy($addonImage);
							if (isset($outfitFiles["addon_{$addonLayer}_t"][$outfitIndex])) {
								$addonMask = imagecreatefrompng($outfitFiles["addon_{$addonLayer}_t"][$outfitIndex]);
								$this->alphaOverlay($image_colorMask, $addonMask);
								imagedestroy($addonMask);
							}
						}
					}

					$this->colorize($image_colorMask, $image_outfit, $colors[0], $colors[1], $colors[2], $colors[3]);

					$width = imagesx($image_outfit);
					$height = imagesy($image_outfit);

					// resize if necessary
					$image_outfitT = imagecreatetruecolor($imageSize, $imageSize);

					// compile the image
					imagefill($image_outfitT, 0, 0, $bgcolor = imagecolorallocate($image_outfitT, self::$transparentBackgroundColor[0], self::$transparentBackgroundColor[1], self::$transparentBackgroundColor[2]));
					imagecopyresampled($image_outfitT, $image_outfit, imagesx($image_outfitT)-$width, imagesy($image_outfitT)-$height, 0, 0, $width, $height, $width, $height);
					imagecolortransparent($image_outfitT, $bgcolor);
					imagealphablending($image_outfitT, false);
					imagesavealpha($image_outfitT, true);
					imagedestroy($image_outfit);

					// destroy the color mask
					if (isset($image_colorMask) && $image_colorMask) {
						imagedestroy($image_colorMask);
					}

					$frames[] = $image_outfitT;
					$durations[] = $frameDuration;
					$currentDuration += $frameDuration;
				}

				++$direction;
			}

			// create the GIF
			$gc = new GifCreator();
			$gc->create($frames, $durations, 0);
			//$image_outfit = imagecreatefromstring($gc->getGif());

			header('Content-type: image/gif');
			echo $gc->getGif();
			exit;

			//return $image_outfit;
		}

		protected function colorizePixel($_color, &$_r, &$_g, &$_b) {
			if ($_color < count(self::$colorPalette))
				$value = self::$colorPalette[$_color];
			else
				$value = 0;
			$ro = ($value & 0xFF0000) >> 16; // rgb outfit
			$go = ($value & 0xFF00) >> 8;
			$bo = ($value & 0xFF);
			$_r = (int) ($_r * ($ro / 255));
			$_g = (int) ($_g * ($go / 255));
			$_b = (int) ($_b * ($bo / 255));
		}

		protected function colorize(&$_image_template, &$_image_outfit, $_head, $_body, $_legs, $_feet) {
			if (!$_image_template) {
				return;
			}

			for ($i = 0; $i < imagesy($_image_template); $i++) {
				for ($j = 0; $j < imagesx($_image_template); $j++) {
					$templatepixel = imagecolorat($_image_template, $j, $i);
					$outfit = imagecolorat($_image_outfit, $j, $i);

					if ($templatepixel == $outfit)
						continue;

					$rt = ($templatepixel >> 16) & 0xFF;
					$gt = ($templatepixel >> 8) & 0xFF;
					$bt = $templatepixel & 0xFF;
					$ro = ($outfit >> 16) & 0xFF;
					$go = ($outfit >> 8) & 0xFF;
					$bo = $outfit & 0xFF;

					if ($rt && $gt && !$bt) { // yellow == head
						$this->colorizePixel($_head, $ro, $go, $bo);
					} else if ($rt && !$gt && !$bt) { // red == body
						$this->colorizePixel($_body, $ro, $go, $bo);
					} else if (!$rt && $gt && !$bt) { // green == legs
						$this->colorizePixel($_legs, $ro, $go, $bo);
					} else if (!$rt && !$gt && $bt) { // blue == feet
						$this->colorizePixel($_feet, $ro, $go, $bo);
					} else {
						continue; // if nothing changed, skip the change of pixel
					}

					imagesetpixel($_image_outfit, $j, $i, imagecolorallocate($_image_outfit, $ro, $go, $bo));
				}
			}
		}

		protected function alphaOverlay(&$destImg, &$overlayImg) {
			if (!$overlayImg) {
				return $destImg;
			}

			$imgW = min(imagesx($destImg), imagesx($overlayImg));
			$imgH = min(imagesy($destImg), imagesy($overlayImg));
			$dstX = imagesx($destImg) - imagesx($overlayImg);
			$dstY = imagesy($destImg) - imagesy($overlayImg);

			for ($y = 0; $y < $imgH; $y++) {
				for ($x = 0; $x < $imgW; $x++) {
					$ovrARGB = imagecolorat($overlayImg, $x, $y);
					$ovrA = ($ovrARGB >> 24) << 1;
					$ovrR = $ovrARGB >> 16 & 0xFF;
					$ovrG = $ovrARGB >> 8 & 0xFF;
					$ovrB = $ovrARGB & 0xFF;

					$change = false;
					if ($ovrA == 0) {
						$dstR = $ovrR;
						$dstG = $ovrG;
						$dstB = $ovrB;
						$change = true;
					} elseif ($ovrA < 254) {
						$dstARGB = imagecolorat($destImg, $dstX + $x, $dstY + $y);
						$dstR = $dstARGB >> 16 & 0xFF;
						$dstG = $dstARGB >> 8 & 0xFF;
						$dstB = $dstARGB & 0xFF;

						$dstR = (($ovrR * (0xFF - $ovrA)) >> 8) + (($dstR * $ovrA) >> 8);
						$dstG = (($ovrG * (0xFF - $ovrA)) >> 8) + (($dstG * $ovrA) >> 8);
						$dstB = (($ovrB * (0xFF - $ovrA)) >> 8) + (($dstB * $ovrA) >> 8);
						$change = true;
					}
					if ($change) {
						$dstRGB = imagecolorallocatealpha($destImg, $dstR, $dstG, $dstB, 0);
						imagesetpixel($destImg, $dstX + $x, $dstY + $y, $dstRGB);
					}
				}
			}
			return $destImg;
		}
	}
?>
