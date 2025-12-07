// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

;; (function ($) {
    $.fn.gleisplanTabs = function (options) {
        const $settings = $.extend({}, options);

        const $cfg = {
            containerRoot: null,
            containerRootId: ''
        };

        return this.each(function () {
            $cfg.containerRoot = $(this);
            $cfg.containerRootId = $(this).attr("id");
            $cfg.containerChildId = $(this).attr("id") + "Child";

            let gleisplaene = [];

            function createOrGetGleisplan(tabname) {
                const n = gleisplaene.length;
                for (let i = 0; i < n; ++i) {
                    if (gleisplaene[i].tabname === tabname) {
                        if (gleisplaene[i] === true) {
                            return gleisplaene[i].instance;
                        }
                    }
                }

                const newGleisplan = $("#" + $cfg.containerChildId)
                    .clone()
                    .removeAttr("id")
                    .removeClass("hidden")
                    .attr("id", "gleisplan_" + tabname);
                newGleisplan.gleisplan();
                const newInstance = {
                    instance: newGleisplan,
                    name: tabname,
                    loaded: true,
                };
                gleisplaene.push(newInstance);
                $("#" + $cfg.containerChildId).parent().append(newGleisplan);
                return newInstance;
            }

            function removeGleisplan(tabname) {
                const newList = [];
                for (let i = 0; i < gleisplaene.length; ++i) {
                    if (gleisplaene[i].name !== tabname) newList.push(gleisplaene[i]);
                }
                return newList;
            }

            function getPreviousTabname(selectedTabname) {
                for (let i = 0; i < gleisplaene.length; ++i) {
                    if (gleisplaene[i].name === selectedTabname)
                        return gleisplaene[i - 1].name;
                }
                return selectedTabname;
            }

            let ind = 2;
            let clickTimer = null;
            function loadTabs() {
                if (!gleisplaene[0]?.loaded) {
                    createOrGetGleisplan("tabPlan1");
                }

                $(function () {
                    $("#" + $cfg.containerRootId).w2tabs({
                        name: $cfg.containerRootId,
                        active: "tabPlan1",
                        tabs: [
                            { id: "tabPlan1", text: "Basisplan", closable: false },
                            //
                            // TODO support for multiple tabs will be scheduled for a later milestone
                            //      much work, and some changes are needed
                            //
                            //{ id: "cmdAdd", text: "+" },
                            { id: "tabScripting", text: 'Skripte', closable: false },
                        ],
                        onClose: function (event) {
                            const previousTabname = getPreviousTabname(event.target);
                            this.click(previousTabname);
                            gleisplaene = removeGleisplan(event.target);
                        },
                        onClick: function (event) {

                            event.done(async () => {

                                let selectedGleisplan = '';

                                if (event.target === "cmdAdd") {
                                    //// disable doubleclick simulation
                                    //clearTimeout(clickTimer);
                                    //clickTimer = null;
                                    //// tab handling
                                    //let id = "tabPlan" + ind;
                                    //this.insert("cmdAdd", { id: id, text: "Plan " + ind, closable: true, });
                                    //createOrGetGleisplan(id);
                                    //selectedGleisplan = id;
                                    //this.click(id);
                                    //ind++;
                                }
                                //
                                // Zeige das HTML vom Skript-Support
                                //
                                else if (event.target === "tabScripting") {
                                    $('#scriptingTab').show();
                                    $('#menu-toggle').hide();
                                    $('#minimap_gleisplan_tabPlan1').hide();

                                    // refresh grid ui
                                    window.hqScriptRunner.refreshTaskGrid();

                                    if (window.__scriptingIsLoaded === true) {
                                        console.log("Scripting is already loaded!");
                                    } else {
                                        await loadScripting();
                                    }
                                }
                                //
                                // Wenn ein Gleisplan existiert, dann nimm diesen und zeig diesen im TabContainer.
                                //
                                else {
                                    $('#scriptingTab').hide();
                                    $('#menu-toggle').show();
                                    $('#minimap_gleisplan_tabPlan1').show();
                                }

                                setTimeout(() => {
                                        $('#tabs_gleisplaeneTabs_tab_tabPlan1 .w2ui-tab')
                                            .html('<i class="fas fa-map"></i> Basisplan');
                                        $('#tabs_gleisplaeneTabs_tab_tabScripting .w2ui-tab')
                                            .html('<i class="fas fa-edit"></i> Skripte');
                                    },
                                    50);

                            });
                        },
                    });

                    $('#tabs_gleisplaeneTabs_tab_tabPlan1 .w2ui-tab').html('<i class="fas fa-map"></i> Basisplan');
                    $('#tabs_gleisplaeneTabs_tab_tabScripting .w2ui-tab').html('<i class="fas fa-edit"></i> Skripte');

                    $('#scriptingTab').hide();
                });
            }

            const child = $('<div>').attr('id', 'gleisplaeneTabsChild');
            $cfg.containerRoot.parent().append(child);

            loadTabs();
        });
    };
})(jQuery);
