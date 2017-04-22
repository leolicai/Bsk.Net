
//var smsApi = "https://wsuat.acxiom.com.cn/lop/wsappservice/LOPWSAppServiceSOAP?wsdl"; //test
//var smsApi = "https://ws.acxiom.com.cn/lop/wsappservice/LOPWSAppServiceSOAP?wsdl"; //Product

var provinces = {};
var cities = {};

var _selectedPkey = "";
var _selectedPname = "";

var _selectedCkey = "";
var _selectedCname = "";

var _posting = false;

var _codePosting = false;

function resetCitySelect(pKey) {
	var html = "";
	var selectedKey = "";
	for(key in cities[pKey]) {
		if(selectedKey.length < 1) {
			selectedKey = key;
		}

		html += "<option value='" + key + "'>" + cities[pKey][key] + "</option>";
	}
	$("#city").html(html);
	_selectedCkey = selectedKey;
	//_selectedCname = cities[pKey][_selectedCkey];
}

function getRegion() {

	$.get('data.json', {}, function(data) {
		var html = "";
		var selectedKey = "";
		for(key in data["86"]) {
			
			if(selectedKey.length < 1) {
				selectedKey = key;
			}

			provinces[key] = data["86"][key];

			html += "<option value='" + key + "'>" + data["86"][key] + "</option>";

			var keyCount = 0;
			var keyValue = '';
			var keyIndex = '';
			for(_subKey in data[key]) {
				keyCount++;
				keyIndex = _subKey;
				keyValue = data[key][_subKey];
			}
		
			if(1 == keyCount && "市辖区" == keyValue) {
				cities[key] = data[keyIndex];
			} else {
				cities[key] = data[key];
			}
		}
		$("#province").html(html);
		_selectedPkey = selectedKey;
		//$("#selectedProvince").text(provinces[_selectedPkey]);
		resetCitySelect(_selectedPkey);
	}, 'json');

}


function fixLayout(layoutWidth) {
	var docWidth = $(document).width();
	var scaled = (docWidth / layoutWidth) * 100;
	$("html").css("font-size", scaled + "px");
}

$(function () {

	fixLayout(568);

	getRegion();

	$("#province").click(function(){
		$("#city").show();
	});
	$("#province").change(function(){
		_selectedPkey = $(this).val();
		_selectedPname = provinces[_selectedPkey];
		$("#selectedProvince").text(provinces[_selectedPkey]);
		resetCitySelect(_selectedPkey);
	});
	$("#city").change(function(){
		_selectedCkey = $("#city").val();
		_selectedCname = cities[_selectedPkey][_selectedCkey];
		$("#selectedCity").text(_selectedCname);
	});

	$("#page3BtnBox").click(function(){
		if(_posting) {
			return ;
		}
		var _name = $("#name").val();
		var _phone = $("#phone").val();
		var _smscode = $("#smscode").val();
		var _address = $("#address").val();
		var _province = _selectedPname;
		var _city = _selectedCname;
		var _postcode = $("#postcode").val();
		var params = {
			name: _name,
			phone: _phone,
			province: _province,
			city: _city,
			address: _address,
			smscode: _smscode,
			postcode: _postcode
		};

        _posting = true;
        $.post("Apply.aspx", params, function (dt) {
            _posting = false;
            if (!dt.success) {
                if (1000 == dt.code || 1001 == dt.code || 1008 == dt.code) {
                    $(".page-box").hide();
                    $("#page6").fadeIn();
                }
                if (1002 == dt.code) {
                    alert("名字不能为空");
                }
                if (1003 == dt.code || 1004 == dt.code || 1005 == dt.code) {
                    alert("省份城市地址信息不全");
                }
                if (1006 == dt.code || 1009 == dt.code) {
                    $(".page-box").hide();
                    $("#page7").fadeIn();
                }
                if (1010 == dt.code) {
                    alert("申请失败, 请重新申请.");
                }
            } else {
                if (dt.code) { // Apply Success
                    $(".page-box").hide();
                    $("#page4").fadeIn();
                } else { // Apply faild
                    $(".page-box").hide();
                    $("#page5").fadeIn();
                }
            }
        }, "json");

	   ga("send", "event", "volcanoFS", "submit", "click");
	});

	$(window).resize(function(){
		fixLayout(568);
	});

	$("#page1").fadeIn();
	ga("send", "pageview", "volcanoFS_p1"); 

	$("#page1TopBox").click(function(){
		$(".page-box").hide();
		$("#page2").fadeIn();
		ga("send", "event", "volcanoFS", "rules", "click");
		ga("send", "pageview", "volcanoFS_p2"); 
	});

	$("#page2BtnBox").click(function(){
		$(".page-box").hide();
		$("#page1").fadeIn();
		ga("send", "event", "volcanoFS", "confirm", "click");
		ga("send", "pageview", "volcanoFS_p1"); 
	});

	$("#page1BottomBox").click(function(){
		$(".page-box").hide();
		$("#page3").fadeIn();
		ga("send", "event", "volcanoFS", "apply", "click");
		ga("send", "pageview", "volcanoFS_p3");
	});

	$("#page3SmsBox").click(function(){

        if (_codePosting) {
            return;
        }

		var phoneNumber = $("#phone").val();

        if (phoneNumber.length != 11) {
            alert("电话号码不合法!");
			return;
		}

        _codePosting = true;
        $.get("VerifyCode.aspx", { phone: phoneNumber }, function (dt) {
            console.log(dt);
            _codePosting = false;
            if (!dt.success) {
                if (1000 == dt.code) {
                    alert("电话号码不能为空或非法的电话号码");
                } else if (1001 == dt.code) {
                    alert("你填写的电话号码格式不对.");
                } else if (1002 == dt.code) {
                    alert("你今天已经申请超过3次, 不能再申请.");
                }
            }
        }, "json");

    });

    $("#page6Alarm").click(function () {
        $(".page-box").hide();
        $("#page3").fadeIn();
    });

    $("#page7Alarm").click(function () {
        $(".page-box").hide();
        $("#page3").fadeIn();
    });
});
