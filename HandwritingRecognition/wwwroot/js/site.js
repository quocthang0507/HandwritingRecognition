// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

$(document).ready(function () {
	var mousePressed = false;
	var lastX, lastY;
	var el = document.getElementById('paint');
	if (!el) return;
	var ctx_paint = el.getContext('2d');
	var slider = document.getElementById("inputWidth");
	var color = document.getElementById("inputColor");
	var output = document.getElementById("widthPaint");
	var chart = document.getElementById('chart');
	let chart_object;
	output.innerHTML = slider.value;

	el.addEventListener('mousedown', function (e) {
		e.preventDefault();
		mousePressed = true;
		draw(e, false);
	});

	el.addEventListener('touchstart', function (e) {
		e.preventDefault();
		mousePressed = true;
		draw(e, false);
	});

	el.addEventListener('mousemove', function (e) {
		e.preventDefault();
		if (mousePressed) {
			draw(e, true);
		}
	});

	el.addEventListener('touchmove', function (e) {
		e.preventDefault();
		if (mousePressed) {
			draw(e, true);
		}
	});

	el.addEventListener('touchend', stopDrawing);
	el.addEventListener('mouseup', stopDrawing);
	el.addEventListener('touchcancel', stopDrawing);
	el.addEventListener('mouseleave', stopDrawing);

	function stopDrawing(e) {
		e.preventDefault();
		mousePressed = false;
	}

	function draw(e, isDown) {
		var x =
			(e.clientX || e.touches[0].clientX) +
			(document.documentElement.scrollLeft || document.body.scrollLeft) -
			el.offsetLeft;
		var y =
			(e.clientY || e.touches[0].clientY) +
			(document.documentElement.scrollTop || document.body.scrollTop) -
			el.offsetTop;
		if (isDown) {
			ctx_paint.beginPath();
			ctx_paint.strokeStyle = color.value;
			ctx_paint.lineWidth = slider.value;
			ctx_paint.lineJoin = 'round';
			ctx_paint.moveTo(lastX, lastY);
			ctx_paint.lineTo(x, y);
			ctx_paint.closePath();
			ctx_paint.stroke();
		}
		lastX = x;
		lastY = y;
	}

	$('#clearArea').click(function () {
		ctx_paint.setTransform(1, 0, 0, 1, 0, 0);
		ctx_paint.clearRect(0, 0, ctx_paint.canvas.width, ctx_paint.canvas.height);
		clearResult();
	});

	$('#check').click(function () {
		$('#prediction').text('?');
		$.ajax({
			type: 'POST',
			url: 'home/upload',
			data: {
				base64Image: el.toDataURL()
			}
		}).done(function (msg) {
			console.log(msg.pixelValues);
			$('#prediction').text(msg.prediction);
			scores = JSON.parse(msg.scores);
			labels = scores.map(function (e) {
				return e.Digit
			});
			data = scores.map(function (e) {
				return e.Score
			});
			displayChart(labels, data, color.value);
		});
	});

	slider.oninput = function () {
		output.innerHTML = this.value;
	}

	function clearResult() {
		chart_object.destroy();
		chart.style.display = 'none';
		$('#prediction').text('?');
	}

	function displayChart(labels, data, color) {
		chart.style.display = '';
		chart_object = new Chart(chart, {
			type: "bar",
			data: {
				labels: labels,
				datasets: [{
					data: data,
					label: 'Score',
					backgroundColor: color
				}]
			},
		});
	}
});
