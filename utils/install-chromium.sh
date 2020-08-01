#!/bin/bash

function ensure_success {
	exit_status=$?
	if [ $exit_status -ne 0 ]; then
		echo "... command exited with code $exit_status"
		exit $exit_status
	fi
}

echo "Installing Chromium prerequisites..."
apt-get -qqy update
apt-get -qqy install lsb-release libappindicator3-1
ensure_success

echo "Downloading Chromium debian package..."
# latest stable: https://dl.google.com/linux/direct/google-chrome-stable_current_amd64.deb
# version 80: https://www.slimjet.com/chrome/download-chrome.php?file=files%2F80.0.3987.149%2Fgoogle-chrome-stable_current_amd64.deb
curl -L -o google-chrome.deb https://www.slimjet.com/chrome/download-chrome.php?file=files%2F80.0.3987.149%2Fgoogle-chrome-stable_current_amd64.deb
ensure_success

echo "Installing Chromium debian package..."
apt -y install ./google-chrome.deb
ensure_success

echo "Patching for running Chromium without sandbox"
sed -i 's|HERE/chrome"|HERE/chrome" --no-sandbox|g' /opt/google/chrome/google-chrome
ensure_success

echo "Clean-up"
rm google-chrome.deb