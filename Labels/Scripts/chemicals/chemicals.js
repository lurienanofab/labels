$.fn.chemicals = function (options) {
    return this.each(function () {
        var $this = $(this);

        var opts = $.extend({}, { "apiurl": "api/", "labelurl": "labels/", "room": null, "clientId": 0 }, options, $this.data());

        var labels = {};

        var printParams = {
            "private-chem-small": { twinTurboRoll: dymo.label.framework.TwinTurboRoll.Right },
            "private-chem-large": { twinTurboRoll: dymo.label.framework.TwinTurboRoll.Left }
        };

        var getChemicalName = function (chem) {
            if (chem.Restricted)
                return "* " + chem.ChemicalName;
            else
                return chem.ChemicalName;
        }

        var loadChemicals = function () {
            var def = $.Deferred();

            $.ajax({
                "url": opts.apiurl + "chemical/" + opts.room,
                "method": "GET"
            }).done(function (data, textStatus, xhr) {
                $(".approved-chemicals", $this).html(
                    $("<option/>", { "value": "0" }).text("-- Select --")
                ).append($.map(data, function (item, index) {
                    return $("<option/>", { "value": item.PrivateChemicalID, "data-restricted": item.Restricted, "data-shared": item.Shared, "data-name": item.ChemicalName }).text(getChemicalName(item));
                }));

                def.resolve();
            }).fail(def.reject);

            return def.promise();
        }

        var loadLocations = function (privateChemicalId) {
            var def = $.Deferred();

            $.ajax({
                "url": opts.apiurl + "chemical/" + opts.room + "/" + privateChemicalId + "/location",
                "method": "GET"
            }).done(function (data, textStatus, xhr) {
                if (data.length == 1) {
                    $(".locations", $this).html($.map(data, function (item, index) {
                        return $("<option/>", { "value": item.LabelLocationID }).text(item.LocationName);
                    }));
                } else {
                    $(".locations", $this).html(
                        $("<option/>", { "value": "0" }).text("-- Select --")
                    ).append($.map(data, function (item, index) {
                        return $("<option/>", { "value": item.LabelLocationID }).text(item.LocationName);
                    }));
                }

                def.resolve();
            }).fail(def.reject);

            return def.promise();
        }

        var loadUsers = function () {
            var def = $.Deferred();

            var getDisplayName = function (user) {
                return user.LName + ", " + user.FName;
            }

            $.ajax({
                "url": opts.apiurl + "user",
                "method": "GET"
            }).done(function (data, textStatus, xhr) {
                $(".users", $this).html(
                    $("<option/>", { "value": "0" }).text("-- Select --")
                ).append($.map(data, function (item, index) {
                    return $("<option/>", { "value": item.ClientID, "data-lname": item.LName, "data-fname": item.FName, "data-phone": item.Phone, "data-email": item.Email, "data-start": moment(item.StartDate).format("MMM YYYY") }).text(getDisplayName(item));
                }));

                def.resolve();
            }).fail(def.reject);

            return def.promise();
        }

        var selectCurrentUser = function () {
            var option = $(".users option[value='" + opts.clientId + "']", $this);

            option.prop("selected", true);

            setContactInfo($.extend({}, { clientId: option.val() }, option.data()));

            if (!opts.staff) {
                $(".users", $this).prop("disabled", true);
            }
        }

        var setContactInfo = function (user) {
            $(".contact-phone", $this).val(user.phone);
            $(".contact-email", $this).val(user.email);
        }

        var loadPrinters = function () {
            var def = $.Deferred();

            // dymo sdk is required
            if (!dymo) {
                alert('DYMO Label Framework not found!');
                def.reject();
            } else {
                dymo.label.framework.getPrintersAsync().then(function (printers) {
                    if (printers.length > 0) {
                        $(".dymo-printers", $this).html($.map(printers, function (item, index) {
                            return $("<option/>").text(item.name);
                        }));

                        def.resolve();
                    } else {
                        $(".dymo-printers", $this).html($("<option/>").text("No printers found.")).prop("disabled", true);
                        def.reject();
                    }
                }).thenCatch(def.reject);
            }

            return def.promise();
        }

        var addLogEntry = function (data) {
            return $.ajax({
                "url": opts.apiurl + "chemical/log",
                "method": "POST",
                "data": {
                    "PrintedByClientID": data.clientId,
                    "LabelClientID": data.user.clientId,
                    "PrivateChemicalID": data.chemical.privateChemicalId,
                    "LabelLocationID": data.location.labelLocationId,
                    "ContactPhone": data.user.phone,
                    "ContactEmail": data.user.email,
                    "StartDate": data.start,
                    "DisposeDate": data.dispose,
                    "StoreStock": data.storeStock
                }
            });
        }

        var getDisplayName = function (user) {
            return user.lname + ", " + user.fname;
        }

        var applyDataToLabel = {
            "getDisplayName": function (user) {
                return user.lname + ", " + user.fname;
            },
            "getLabelTitle": function (data) {
                if (data.storeStock)
                    return "Store Chemical";
                else if (data.chemical.shared)
                    return "Shared Chemical";
                else
                    return "Private Chemical";
            },
            "test": function (check, trueVal, falseVal) {
                if (check)
                    return trueVal;
                else
                    return falseVal;
            },
            "formatDate": function (val, format) {
                return moment(val).format(format);
            },
            "private-chem-large": function (label, data) {
                label.setObjectText("LabelTitle", this.getLabelTitle(data));
                label.setObjectText("ChemicalName", data.chemical.name);
                label.setObjectText("DisplayName", this.getDisplayName(data.user));
                label.setObjectText("PhoneOrExpireLabel", this.test(data.storeStock, "Expire Date", "Contact Phone"));
                label.setObjectText("PhoneOrExpiry", data.user.phone);
                label.setObjectText("EmailAddress", data.user.email);
                label.setObjectText("StartDate", this.formatDate(data.start, "MM/DD/YYYY"));
                label.setObjectText("DisposeDate", this.test(data.storeStock, " ", this.formatDate(data.dispose, "[Chemical to be disposed of on: ]MM/DD/YYYY")));
                label.setObjectText("Location", data.location.name);
            },
            "private-chem-small": function (label, data) {
                label.setObjectText("LabelTitle", this.getLabelTitle(data));
                label.setObjectText("ChemicalName", data.chemical.name);
                label.setObjectText("DisplayName", this.getDisplayName(data.user));
                label.setObjectText("PhoneOrExpireLabel", this.test(data.storeStock, "Expire", "Phone"));
                label.setObjectText("PhoneOrExpiry", data.user.phone);
                label.setObjectText("EmailAddress", "Email: " + data.user.email);
                label.setObjectText("StartDate", this.formatDate(data.start, "[Start: ]MM/DD/YYYY"));
                label.setObjectText("DisposeDate", this.test(data.storeStock, " ", this.formatDate(data.dispose, "[Dispose On: ]MM/DD/YYYY")));
                label.setObjectText("Location", data.location.name);
            }
        };

        var getLabel = function (file, labelData) {
            var def = $.Deferred();

            var labelUrl = opts.labelurl + file + ".xml";

            var label = labels[file];

            if (label) {
                applyDataToLabel[file](label, labelData);
                def.resolve(label);
            } else {
                $.ajax({
                    "url": labelUrl,
                    "method": "GET",
                    "dataType": "text"
                }).done(function (data) {
                    label = dymo.label.framework.openLabelXml(data);
                    applyDataToLabel[file](label, labelData);
                    labels[file] = label;
                    def.resolve(label);
                }).fail(def.reject);
            }

            return def.promise();
        }

        var showPreview = function (file, labelData) {
            var preview = $(".preview-panel .label-preview", $this);

            preview.html($("<div/>", { "class": "loader" }));

            getLabel(file, labelData).done(function (label) {
                var renderParams = dymo.label.framework.createLabelRenderParamsXml({ shadowColor: { alpha: 0, red: 170, green: 170, blue: 170 }, shadowDepth: 50 });
                label.renderAsync(renderParams).then(function (pngData) {
                    preview.html($("<img/>", { "src": "data:image/png;base64," + pngData }));
                }).thenCatch(function (err) {
                    console.log(err);
                });
            });
        }

        var getChemical = function (option) {
            var result = {
                "privateChemicalId": 0,
                "restricted": false,
                "shared": false,
                "name": ""
            };

            if (option.length > 0)
                $.extend(result, option.data(), {
                    "privateChemicalId": option.val()
                });

            return result;
        }

        var getUser = function (option, override) {
            var result = {
                "clientId": 0,
                "lname": "",
                "fname": "",
                "phone": "",
                "email": "",
                "start": ""
            };

            // when override is true use the option data to get phone and email
            // when override is false use the contact-phone and contact-email text boxes

            if (option.length > 0) {
                if (!override)
                    $.extend(result, option.data(), {
                        "clientId": option.val(),
                        "phone": $(".contact-phone", $this).val(),
                        "email": $(".contact-email", $this).val()
                    });
                else
                    $.extend(result, option.data(), {
                        "clientId": option.val()
                    });
            }

            return result;
        }

        var getLocation = function (option) {
            var result = {
                "labelLocationId": 0,
                "name": ""
            };

            if (option.length > 0)
                $.extend(result, { "labelLocationId": option.val(), "name": option.text() });

            return result;
        }

        var getLabelData = function () {
            return {
                "clientId": opts.clientId,
                "start": opts.start,
                "dispose": opts.dispose,
                "storeStock": $(".store-stock", $this).prop("checked") === true,
                "chemical": getChemical($(".approved-chemicals", $this).find("option:selected")),
                "user": getUser($(".users", $this).find("option:selected"), false),
                "location": getLocation($(".locations", $this).find("option:selected"))
            };
        }

        var validate = function (data) {
            var errors = 0;

            $(".form-group.has-error", $this).removeClass("has-error");

            if (data.chemical.privateChemicalId == 0) {
                errors++;
                $(".approved-chemicals", $this).closest(".form-group").addClass("has-error");
            }

            if (data.location.labelLocationId == 0) {
                errors++;
                $(".locations", $this).closest(".form-group").addClass("has-error");
            }

            if (data.user.clientId == 0) {
                errors++;
                $(".users", $this).closest(".form-group").addClass("has-error");
            }


            if (!data.user.phone) {
                errors++;
                $(".contact-phone", $this).closest(".form-group").addClass("has-error");
            }

            if (!data.user.email) {
                errors++;
                $(".contact-email", $this).closest(".form-group").addClass("has-error");
            }

            return errors === 0;
        }

        loadChemicals().done(function () {
            loadUsers().done(function () {
                selectCurrentUser();
                loadPrinters();
            });
        });

        $this.on("change", ".store-stock", function (e) {
            if ($(this).prop("checked") === true) {
                $(".phone-label", $this).text("Expire Date");
                $(".contact-phone", $this).val(moment(opts.dispose).format("MM/DD/YYYY"));
            }
            else {
                var user = getUser($(".users", $this).find("option:selected"), true);
                $(".phone-label", $this).text("Contact Phone");
                $(".contact-phone", $this).val(user.phone);
            }
        }).on("change", ".approved-chemicals", function (e) {
            var option = $(this).find("option:selected");
            var chem = getChemical(option);
            loadLocations(chem.privateChemicalId).done(function () {
                $(".restricted-message", $this).toggle(chem.restricted);
            });
        }).on("change", ".users", function (e) {
            var option = $(this).find("option:selected");
            var user = getUser(option, true);
            setContactInfo(user);
        }).on("click", ".preview", function (e) {
            var labelData = getLabelData();
            if (validate(labelData)) {
                var file = $(this).data("file");
                showPreview(file, labelData);
            }
        }).on("click", ".print", function (e) {
            var file = $(this).data("file");
            var printer = $(".dymo-printers option:selected", $this).text();

            var labelData = getLabelData();

            if (validate(labelData)) {
                getLabel(file, labelData).done(function (label) {
                    var pp = dymo.label.framework.createLabelWriterPrintParamsXml(printParams[file]);
                    label.printAsync(printer, pp).then(function () {
                        addLogEntry(labelData);
                    });
                });
            }
        }).on("add-log-entry", function (e) {
            var labelData = getLabelData();
            if (validate(labelData)) {
                addLogEntry(labelData);
            }
        }).on("label-data", function (e, callback) {
            var labelData = getLabelData();
            if (typeof callback == "function")
                callback(labelData);
        });
    });
}